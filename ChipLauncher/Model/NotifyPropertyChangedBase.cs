using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChipLauncher
{
    public class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void SetValue<T>(ref T storageVariable, T value, [CallerMemberName] string propertyName = "")
        {
            SetValue(propertyName, ref storageVariable, value);
        }

        protected virtual void SetValue<T>(string propertyName, ref T storageVariable, T value)
        {
            storageVariable = value;
            OnPropertyChanged(propertyName);
        }
    }
}
