using System;
using System.Windows.Input;

namespace Tanji.Helpers
{
    public class Command : ICommand
    {
        private readonly Action<object> _executeDelegate;
        private readonly Predicate<object> _canExecuteDelegate;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        
        public Command(Action<object> execute)
        {
            _executeDelegate = execute;
        }
        public Command(Action<object> execute, Predicate<object> canExecute)
            : this(execute)
        {
            _canExecuteDelegate = canExecute;
        }

        public void Execute(object parameter)
        {
            _executeDelegate(parameter);
        }
        public bool CanExecute(object parameter)
        {
            return (_canExecuteDelegate?.Invoke(parameter) ?? true);
        }
    }
}