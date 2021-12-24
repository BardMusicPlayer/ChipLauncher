using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace XIVLauncher
{
    static class XIVGame
    {
        public static string GetFFXIVInstallPath()
        {
            string GetFullPath(string input)
            {
                return Path.GetFullPath(Path.Combine(input, "../../")).Replace("\\", "/");
            };

            string p_name = "{2B41E132-07DF-4925-A3D3-F2D1765CCDFE}";
            string displayName;
            RegistryKey key;

            // search in: CurrentUser
            key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (String keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey.GetValue("ProductGuid") as string;
                if (p_name.Equals(displayName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return GetFullPath(subkey.GetValue("DisplayIcon") as string);
                }
            }

            // search in: LocalMachine_32
            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (String keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey.GetValue("ProductGuid") as string;
                if (p_name.Equals(displayName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return GetFullPath(subkey.GetValue("DisplayIcon") as string);
                }
            }

            // search in: LocalMachine_64
            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (String keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey.GetValue("ProductGuid") as string;
                if (p_name.Equals(displayName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return GetFullPath(subkey.GetValue("DisplayIcon") as string);
                }
            }

            // NOT FOUND
            return "UNKNOWN";
        }

        public static string GamePath { get; set; }

        private static string gamePath { get { return GamePath; } }

        private static readonly string UserAgentTemplate = "SQEXAuthor/2.0.0(Windows 6.2; ja-jp; {0})";

        private static readonly string UserAgent = GenerateUserAgent();
        
        /// <summary>
        /// Launches FFXIV with the supplied parameters.
        /// </summary>
        /// <param name="realsid">Real SessionID</param>
        /// <param name="language">language(0=japanese,1=english,2=french,3=german)</param>
        /// <param name="dx11">Runs the game in dx11 mode if true</param>
        /// <param name="expansionlevel">current level of expansions loaded(0=ARR/default,1=Heavensward)</param>
        public static Process LaunchGame(string realsid, int language, bool dx11, int expansionlevel, bool isSteam)
        {
            try
            {
                Process ffxivgame = new Process();
                ffxivgame.StartInfo.FileName = gamePath+"/game/ffxiv_dx11.exe";
                ffxivgame.StartInfo.Arguments = $"DEV.TestSID={realsid} DEV.MaxEntitledExpansionID={expansionlevel} language={language} region=3";
                if (isSteam)
                {
                    ffxivgame.StartInfo.Environment.Add("IS_FFXIV_LAUNCH_FROM_STEAM", "1");
                    ffxivgame.StartInfo.Arguments += " IsSteam=1";
                    ffxivgame.StartInfo.UseShellExecute = false;
                }
                ffxivgame.Start();
                return ffxivgame;
            }
            catch (Exception exc)
            {
                Console.WriteLine("Could not launch executable. Is your game path correct? " + exc);
            }

            return null;
        }

        /// <summary>
        /// Gets a real SessionID for the supplied credentials.
        /// </summary>
        /// <param name="username">Square Enix ID</param>
        /// <param name="password">Password</param>
        /// <param name="otp">OTP</param>
        /// <returns></returns>
        public static string GetRealSid(string username, string password, string otp, bool isSteam)
        {
            string hashstr = "";
            try
            {
                // make the string of hashed files to prove game version//make the string of hashed files to prove game version
                hashstr = "ffxivboot.exe/" + GenerateHash(gamePath + "/boot/ffxivboot.exe") +
                          ",ffxivboot64.exe/" + GenerateHash(gamePath + "/boot/ffxivboot64.exe") +
                          ",ffxivlauncher.exe/" + GenerateHash(gamePath + "/boot/ffxivlauncher.exe") +
                          ",ffxivlauncher64.exe/" + GenerateHash(gamePath + "/boot/ffxivlauncher64.exe") +
                          ",ffxivupdater.exe/" + GenerateHash(gamePath + "/boot/ffxivupdater.exe") +
                          ",ffxivupdater64.exe/" + GenerateHash(gamePath + "/boot/ffxivupdater64.exe"); 
            }
            catch (Exception exc)
            {
                MessageBox.Show("Could not generate hashes. Is your game path correct? " + exc);
            }

            WebClient sidClient = new WebClient();
            sidClient.Headers.Add("X-Hash-Check", "enabled");
            sidClient.Headers.Add("user-agent", UserAgent);
            sidClient.Headers.Add("Referer", "https://ffxiv-login.square-enix.com/oauth/ffxivarr/login/top?lng=en&rgn=3");
            sidClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            InitiateSslTrust();

            try
            {
                var localGameVer = GetLocalGamever();
                var localSid = GetSid(username, password, otp, isSteam);

                if (localGameVer.Equals("BAD") || localSid.Equals("BAD"))
                {
                    return "BAD";
                }

                var url = "https://patch-gamever.ffxiv.com/http/win32/ffxivneo_release_game/" + localGameVer + "/" + localSid;
                sidClient.UploadString(url, hashstr); //request real session id
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Unable to retrieve a session ID from the server.\n" + exc);
            }

            return sidClient.ResponseHeaders["X-Patch-Unique-Id"];
        }


        private static string GetStored(bool isSteam) //this is needed to be able to access the login site correctly
        {
            WebClient loginInfo = new WebClient();
            loginInfo.Headers.Add("user-agent", UserAgent);
            string reply = loginInfo.DownloadString(string.Format("https://ffxiv-login.square-enix.com/oauth/ffxivarr/login/top?lng=en&rgn=3&isft=0&issteam={0}", isSteam ? 1 : 0));

            Regex storedre = new Regex(@"\t<\s*input .* name=""_STORED_"" value=""(?<stored>.*)"">");

            var stored = storedre.Matches(reply)[0].Groups["stored"].Value;
            return stored;
        }

        public static string GetSid(string username, string password, string otp, bool isSteam)
        {
            using (WebClient loginData = new WebClient())
            {
                loginData.Headers.Add("user-agent", UserAgent);
                loginData.Headers.Add("Referer", string.Format("https://ffxiv-login.square-enix.com/oauth/ffxivarr/login/top?lng=en&rgn=3&isft=0&issteam={0}", isSteam ? 1 : 0));
                loginData.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                try
                {
                    byte[] response =
                        loginData.UploadValues("https://ffxiv-login.square-enix.com/oauth/ffxivarr/login/login.send", new NameValueCollection() //get the session id with user credentials
                        {
                            { "_STORED_", GetStored(isSteam) },
                            { "sqexid", username },
                            { "password", password },
                            { "otppw", otp }
                        });

                    string reply = System.Text.Encoding.UTF8.GetString(response);
                    Console.WriteLine(reply);
                    Regex sidre = new Regex(@"sid,(?<sid>.*),terms");
                    var matches = sidre.Matches(reply);
                    if (matches.Count == 0)
                    {
                        if (reply.Contains("ID or password is incorrect"))
                        {
                            MessageBox.Show("Incorrect username or password.", "Error", MessageBoxButton.OK);
                            return "BAD";
                        }
                    }

                    var sid = sidre.Matches(reply)[0].Groups["sid"].Value;
                    return sid;
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Something failed when attempting to request a session ID.\n" + exc);
                    return "BAD";
                }
            }
        }

        private static string GetLocalGamever()
        {
            try
            {
                using (StreamReader sr = new StreamReader(gamePath + @"/game/ffxivgame.ver"))
                {
                    string line = sr.ReadToEnd();
                    return line;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Unable to get local game version.\n" + exc);
                return "BAD";
            }
        }

        private static string GenerateHash(string file)
        {
            byte[] filebytes = File.ReadAllBytes(file);

            var hash = (new SHA1Managed()).ComputeHash(filebytes);
            string hashstring = string.Join("", hash.Select(b => b.ToString("x2")).ToArray());

            long length = new FileInfo(file).Length;

            return length + "/" + hashstring;
        }

        public static bool GetGateStatus()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string reply = client.DownloadString("http://frontier.ffxiv.com/worldStatus/gate_status.json");

                    return Convert.ToBoolean(int.Parse(reply[10].ToString()));
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Failed getting gate status. " + exc);
                return false;
            }

        }

        private static void InitiateSslTrust()
        {
            //Change SSL checks so that all checks pass, squares gamever server does strange things
            ServicePointManager.ServerCertificateValidationCallback =
                new RemoteCertificateValidationCallback(
                    delegate
                    { return true; }
                );
        }


        private static string GenerateUserAgent()
        {
            return string.Format(UserAgentTemplate, MakeComputerId());
        }

        private static string MakeComputerId()
        {
            var hashString = Environment.MachineName + Environment.UserName + Environment.OSVersion +
                             Environment.ProcessorCount;

            using (var sha1 = HashAlgorithm.Create("SHA1"))
            {
                var bytes = new byte[5];

                Array.Copy(sha1.ComputeHash(Encoding.Unicode.GetBytes(hashString)), 0, bytes, 1, 4);

                var checkSum = (byte) -(bytes[1] + bytes[2] + bytes[3] + bytes[4]);
                bytes[0] = checkSum;

                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }



}
