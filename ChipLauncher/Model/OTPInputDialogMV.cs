using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChipLauncher.Model
{
    internal class OTPInputDialogMV : NotifyPropertyChangedBase
    {
        public OTPInputDialogMV()
        {
            PropertyChanged += (o, e) =>
            {
                //ConfirmOTPIsValid;
            };
        }

        public string UserInputOTPString
        {
            get => _userInputOTPString;
            set => SetValue(ref _userInputOTPString, value);
        }

        public ICommand CancelOTPButtonCommand
        {
            get
            {
                return new RelayCommand(
                    (obj) =>
                    {
                        return true;
                    },
                    (obj) =>
                    {
                        // do nothing
                        UserInputOTPString = string.Empty;
                        (obj as ICloseable).Close();
                    }
                );
            }
        }

        public ICommand ConfirmOTPButtonCommand
        {
            get
            {
                return new RelayCommand(
                    (obj) =>
                    {
                        return ConfirmOTPIsValid(UserInputOTPString);
                    },
                    (obj) =>
                    {
                        (obj as ICloseable).Close();
                    }
                );
            }
        }

        private bool ConfirmOTPIsValid(string input)
        {
            if (input == null) return false;
            if (input.Length != 6) return false;
            return int.TryParse(input, out _);
        }

        private string _userInputOTPString;
    }
}
