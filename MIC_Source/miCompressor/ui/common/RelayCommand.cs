using System;
using System.Windows.Input;

namespace miCompressor.ui
{
    /// <summary>
    /// A generic command implementation that enables binding UI actions to ViewModel methods.
    /// Implements ICommand to support MVVM-style command binding in WinUI 3.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <example>
    /// Example usage in a ViewModel:
    /// <code>
    /// public ICommand SetQualityCommand =&gt; new RelayCommand&gt;object&gt;(param =&gt;
    /// {
    ///     if (param is string paramValue) 
    ///     {
    ///         // param is object, cast as required. 
    ///     }
    /// });
    /// </code>
    /// Example usage in XAML:
    /// <code>
    /// <Button Content="Save" Command="{x:Bind ViewModel.SaveCommand}" />
    /// </code>
    /// </example>
    public class RelayCommand<T> : ICommand
    {
        /// <summary>
        /// The action to execute when the command is invoked.
        /// </summary>
        private readonly Action<T> _execute;

        /// <summary>
        /// A predicate to determine whether the command can execute.
        /// If null, the command is always executable.
        /// </summary>
        private readonly Func<T, bool>? _canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The action to execute.</param>
        /// <param name="canExecute">Optional predicate to determine if the command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if execute is null.</exception>
        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <returns>True if the command can execute, otherwise false.</returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || (parameter is T typedParameter && _canExecute(typedParameter));
        }

        /// <summary>
        /// Executes the command with the provided parameter.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        public void Execute(object? parameter)
        {
            if (parameter is T typedParameter)
            {
                _execute(typedParameter);
            }
        }

        /// <summary>
        /// Event triggered when the ability to execute the command changes.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Raises the CanExecuteChanged event to notify the UI to refresh command state.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
