using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FloatTodo.App.Models;

namespace FloatTodo.App.ViewModels;

/// <summary>
/// View model for the main todo shell. The data is kept in memory only for this phase.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private string _newTaskTitle = string.Empty;
    private string _newTaskDescription = string.Empty;
    private TaskPriority _newTaskPriority = TaskPriority.Normal;
    private DateTime? _newTaskDueTime;

    public MainViewModel()
    {
        AddTaskCommand = new RelayCommand(_ => AddTask(), _ => CanAddTask());
        DeleteTaskCommand = new RelayCommand(parameter => DeleteTask((TaskItem)parameter!), _ => true);
        CompleteTaskCommand = new RelayCommand(parameter => CompleteTask((TaskItem)parameter!), _ => true);
    }

    public ObservableCollection<TaskItem> Tasks { get; } = [];

    public TaskPriority[] Priorities { get; } = Enum.GetValues<TaskPriority>();

    public string NewTaskTitle
    {
        get => _newTaskTitle;
        set
        {
            if (_newTaskTitle != value)
            {
                _newTaskTitle = value;
                OnPropertyChanged();
                ((RelayCommand)AddTaskCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string NewTaskDescription
    {
        get => _newTaskDescription;
        set
        {
            if (_newTaskDescription != value)
            {
                _newTaskDescription = value;
                OnPropertyChanged();
            }
        }
    }

    public TaskPriority NewTaskPriority
    {
        get => _newTaskPriority;
        set
        {
            if (_newTaskPriority != value)
            {
                _newTaskPriority = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? NewTaskDueTime
    {
        get => _newTaskDueTime;
        set
        {
            if (_newTaskDueTime != value)
            {
                _newTaskDueTime = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand AddTaskCommand { get; }

    public ICommand DeleteTaskCommand { get; }

    public ICommand CompleteTaskCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool CanAddTask()
    {
        return !string.IsNullOrWhiteSpace(NewTaskTitle);
    }

    private void AddTask()
    {
        if (!CanAddTask())
        {
            return;
        }

        // Create a new task in memory so the first stage can stay lightweight.
        var task = new TaskItem
        {
            Title = NewTaskTitle.Trim(),
            Description = NewTaskDescription.Trim(),
            Priority = NewTaskPriority,
            DueTime = NewTaskDueTime,
            Status = FloatTodo.App.Models.TaskStatus.Todo
        };

        Tasks.Insert(0, task);
        ClearNewTaskForm();
    }

    private void DeleteTask(TaskItem? task)
    {
        if (task is not null)
        {
            Tasks.Remove(task);
        }
    }

    private void CompleteTask(TaskItem? task)
    {
        if (task is null)
        {
            return;
        }

        // Marking the task as done updates the card state immediately.
        task.Status = FloatTodo.App.Models.TaskStatus.Done;
        task.CompletedAt = DateTime.Now;
    }

    private void ClearNewTaskForm()
    {
        NewTaskTitle = string.Empty;
        NewTaskDescription = string.Empty;
        NewTaskPriority = TaskPriority.Normal;
        NewTaskDueTime = null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Predicate<object?> _canExecute;
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute, Predicate<object?> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
