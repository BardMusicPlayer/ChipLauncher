using System;
using System.Windows;

namespace ChipLauncher
{
    using MahApps.Metro.Controls;
    using Microsoft.Win32;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using XIVLauncher;

    public partial class MainWindow : MetroWindow
    {
        private bool AdvancedModeEnabled = false;

        private class CharacterData
        {
            public string username { get; set; }
            public string password { get; set; }
            public string otp { get; set; }
            public bool isSteam { get; set; }
            public int[] cpuIds { get; set; }
            public ProcessPriorityClass priority { get; set; }
            public string gamepath { get; set; }
        }

        private class AppConfig
        {
            public string GamePath { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            this.Width = 280;
            this.ShowTitleBar = true;
            this.Title = "ChipLauncher v1.0";

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                // System.Threading.Thread.Sleep(1 * 1000);

                this.Dispatcher.Invoke(() =>
                {
                    string[] args = Environment.GetCommandLineArgs();
                    if (args.Length == 2)
                    {
                        this.Hide();
                        btn_ShowAdvanced_Click(null, null);

                        string jsonStr = File.ReadAllText(args[1]);
                        LoadInputFromJson(jsonStr);

                        if (!CheckAndSetGamePath())
                        {
                            App.Current.Shutdown();
                            return;
                        }
                        Login(CreateCharacterFromInput());
                    }
                });
            });
        }

        private void Login(CharacterData data)
        {
            if (!XIVGame.GetGateStatus())
            {
                MessageBox.Show("Square Enix seems to be running maintenance work right now. The game shouldn't be launched.", "Error", MessageBoxButton.OK);
                return;// false;
            }

            try
            {
                var sid = XIVGame.GetRealSid(data.username, data.password, data.otp, data.isSteam);
                if (sid.Equals("BAD"))
                    return;

                var ffxivGame = XIVGame.LaunchGame(sid, 1, true, 0, data.isSteam);
                if (ffxivGame != null && this.AdvancedModeEnabled)
                    EscalateProcess(ffxivGame, data.cpuIds, data.priority);

                App.Current.Shutdown();
            }
            catch (Exception exc)
            {
                MessageBox.Show("Logging in failed, check your login information or try again.\n" + exc.Message, "Error", MessageBoxButton.OK);
            }
        }

        private void EscalateProcess(Process ffxivGame, int[] cpuIds, ProcessPriorityClass priority)
        {
            try
            {
                if (cpuIds != null) ffxivGame.ProcessorAffinity = (IntPtr)GetMaskFromCpuIds(cpuIds);
                ffxivGame.PriorityClass = priority;
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Unable to set advanced features: {0}", e.Message), "Error", MessageBoxButton.OK);
            }
        }

        private string AskUserToManuallySetGamePath()
        {
            string gamePath = "UNKNOWN";

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.RestoreDirectory = true;
            openDialog.FileName = "ffxivboot.exe";
            openDialog.DefaultExt = "exe";
            openDialog.Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*";
            if (openDialog.ShowDialog() ?? false && !string.IsNullOrEmpty(openDialog.FileName))
            {
                gamePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(openDialog.FileName), "../")).Replace("\\", "/");
            }

            return gamePath;
        }

        private bool CheckAndSetGamePath()
        {
            if (Directory.Exists(XIVGame.GamePath))
            {
                if (!File.Exists(Path.Combine(XIVGame.GamePath, "boot/ffxivboot.exe")))
                {
                    MessageBox.Show($"GamePath is a valid directory, but not the correct one. Going to reset this. {XIVGame.GamePath}", "Error", MessageBoxButton.OK);
                    XIVGame.GamePath = string.Empty;
                }
            }

            AppConfig config = new AppConfig();

            // check directory
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "ChipLauncher";
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            // check file
            var configFile = appDataDir + Path.DirectorySeparatorChar + "settings.json";
            if (!File.Exists(configFile))
            {
                string searchedGamePath = XIVGame.GetFFXIVInstallPath();
                if (searchedGamePath.Equals("UNKNOWN"))
                {
                    MessageBox.Show($"Unable to auto-detect install path for FFXIV. Please navigate to your ffxivboot.exe.", "Error", MessageBoxButton.OK);
                    config.GamePath = AskUserToManuallySetGamePath();
                }
                string js = JsonSerializer.Serialize(config);
                File.WriteAllText(configFile, js);
            }

            // check final game path
            string jsonStr = File.ReadAllText(configFile);
            config = JsonSerializer.Deserialize<AppConfig>(jsonStr);
            if (!Directory.Exists(config.GamePath))
            {
                MessageBox.Show($"Unable to detect install path for FFXIV. Please add correct path to FFXIV in settings.json at {configFile}", "Error", MessageBoxButton.OK);
                return false;
            }

            XIVGame.GamePath = config.GamePath;
            return true;
        }

        private void btn_Launch_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckAndSetGamePath())
            {
                return;
            }

            CharacterData data = CreateCharacterFromInput();
            this.Login(data);
        }

        private void btn_ShowAdvanced_Click(object sender, RoutedEventArgs e)
        {
            if (this.AdvancedModeEnabled == true)
            {
                this.Width = 280;
            }
            else
            {
                this.Width = 560;
            }

            this.AdvancedModeEnabled = !this.AdvancedModeEnabled;
        }

        private CharacterData CreateCharacterFromInput()
        {
            CharacterData data = new CharacterData();
            data.username = tbox_Username.Text;
            data.password = pbox_Password.Password;
            data.otp = tbox_OTP.Text;
            data.isSteam = cbox_IsSteam.IsChecked.GetValueOrDefault();
            data.cpuIds = tbox_CpuAffinity.Text.Length > 0 ? Array.ConvertAll(tbox_CpuAffinity.Text.Split(','), int.Parse) : null;
            data.priority = (ProcessPriorityClass)combo_Priority.SelectedValue;
            data.gamepath = XIVGame.GamePath;
            return data;
        }

        private static ulong GetMaskFromCpuIds(params int[] ids)
        {
            ulong mask = 0;
            foreach (int id in ids)
            {
                if (id < 0 || id >= Environment.ProcessorCount)
                    throw new ArgumentOutOfRangeException("CPUId", id.ToString());
                mask |= 1UL << id;
            }
            return mask;
        }

        public Dictionary<ProcessPriorityClass, string> PriorityEnumsWithCaption { get; } =
            new Dictionary<ProcessPriorityClass, string>()
            {
                { ProcessPriorityClass.RealTime,    "Real Time"    },
                { ProcessPriorityClass.High,        "High"         },
                { ProcessPriorityClass.AboveNormal, "Above Normal" },
                { ProcessPriorityClass.Normal,      "Normal"       },
                { ProcessPriorityClass.BelowNormal, "Below Normal" },
                { ProcessPriorityClass.Idle,        "Idle"         },
            };

        private void LoadInputFromJson(string jsonStr)
        {
            CharacterData data = JsonSerializer.Deserialize<CharacterData>(jsonStr);
            if (data != null)
            {
                tbox_Username.Text = data.username;
                pbox_Password.Password = data.password;
                tbox_OTP.Text = data.otp;
                cbox_IsSteam.IsChecked = data.isSteam;
                tbox_CpuAffinity.Text = data.cpuIds.Length > 0 ? string.Join(",", data.cpuIds) : string.Empty;
                combo_Priority.SelectedValue = (ProcessPriorityClass)data.priority;
                XIVGame.GamePath = data.gamepath;
            }
        }

        private void btn_LoadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.RestoreDirectory = true;
            openDialog.DefaultExt = "json";
            openDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
            if (openDialog.ShowDialog() ?? false && !string.IsNullOrEmpty(openDialog.FileName))
            {
                string jsonStr = File.ReadAllText(openDialog.FileName);
                LoadInputFromJson(jsonStr);
            }
        }

        private void btn_SaveFile_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("Your password will be stored in plain text. Are you okay with this?", "Unimplemented", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.RestoreDirectory = true;
                saveDialog.FileName = tbox_Username.Text + ".json";
                saveDialog.DefaultExt = "json";
                saveDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                if (saveDialog.ShowDialog() ?? false && !string.IsNullOrEmpty(saveDialog.FileName))
                {
                    CharacterData data = CreateCharacterFromInput();
                    string jsonStr = JsonSerializer.Serialize(data);
                    File.WriteAllText(saveDialog.FileName, jsonStr);
                }
            }
        }
    }
}
