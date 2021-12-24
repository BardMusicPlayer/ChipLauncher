using System;
using System.Windows.Input;

namespace ChipLauncher
{
    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add    { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute)
            : this(null, execute)
        {
            // do nothing
        }

        public RelayCommand(Predicate<object> canExecute, Action<object> execute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
    }
}
