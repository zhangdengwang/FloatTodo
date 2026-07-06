using System;
using System.Windows.Input;

namespace FloatTodo.App.ViewModels;

/// <summary>
/// Lightweight ICommand implementation used across view models.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Predicate<object?> _canExecute;
    private readonly Action<object?> _execute;

    public RelayCommand(Action<object?> execute, Predicate<object?> canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute(parameter);

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
