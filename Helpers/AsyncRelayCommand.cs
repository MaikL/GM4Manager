using System.Diagnostics;
using System.Windows.Input;

namespace GM4ManagerWPF.Helpers
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        /// <summary>
        /// Initializes a new instance of the AsyncRelayCommand class.
        /// </summary>
        /// <param name="execute">The asynchronous operation to execute.</param>
        /// <param name="canExecute">The function that determines whether the command can execute.</param>
        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public async void Execute(object? parameter)
        {
            try
            {
                await _execute();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Async command failed: " + ex.Message);
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

}
