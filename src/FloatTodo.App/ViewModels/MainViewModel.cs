using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FloatTodo.App.Models;
using FloatTodo.App.Services;

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

    private readonly TaskStorageService _storage;
    private readonly ApiSettingsService _apiSettingsService;

    public MainViewModel()
    {
        _storage = new TaskStorageService();
        _apiSettingsService = new ApiSettingsService();

        AddTaskCommand = new RelayCommand(_ => AddTask(), _ => CanAddTask());
        DeleteTaskCommand = new RelayCommand(parameter => DeleteTask((TaskItem)parameter!), _ => true);
        CompleteTaskCommand = new RelayCommand(parameter => CompleteTask((TaskItem)parameter!), _ => true);

        Tasks = new ObservableCollection<TaskItem>();
        ProjectProgressList = new ObservableCollection<ProjectProgressItem>();

        // Keep progress in sync when tasks collection changes
        Tasks.CollectionChanged += TasksOnCollectionChanged;

        // Load persisted tasks on startup. Failures fall back to empty list.
        try
        {
            var items = _storage.Load();
            foreach (var t in items)
            {
                Tasks.Add(t);
            }
            // Attach handlers for loaded tasks
            foreach (var t in Tasks)
            {
                AttachTaskHandler(t);
            }
        }
        catch
        {
            // Swallow any load errors to keep app running with empty tasks.
        }
        // Initialize daily records sub-viewmodel (keeps daily records separate).
        DailyRecords = new DailyRecordsViewModel();
        AiSettings = new ApiSettingsViewModel(_apiSettingsService);
        AiPlanner = new AiPlannerViewModel(this, new AiPlannerService(_apiSettingsService));

        // initial calculation
        RecalculateProjectProgress();
    }

    public ObservableCollection<ProjectProgressItem> ProjectProgressList { get; }

    public bool HasProjectProgress => ProjectProgressList.Any();

    public ObservableCollection<TaskItem> Tasks { get; }

    // Expose daily records view model to the view so the UI can bind to it.
    public DailyRecordsViewModel DailyRecords { get; }

    public ApiSettingsViewModel AiSettings { get; }

    public TaskPriority[] Priorities { get; } = Enum.GetValues<TaskPriority>();

    // Expose AI planner view model so the view can bind to it.
    public AiPlannerViewModel AiPlanner { get; }

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
            Status = FloatTodo.App.Models.TaskStatus.Todo,
            IsProject = false,
            ParentId = null
        };

        Tasks.Insert(0, task);
        AttachTaskHandler(task);
        ClearNewTaskForm();
        SaveTasks();
        RecalculateProjectProgress();
    }

    private void DeleteTask(TaskItem? task)
    {
        if (task is not null)
        {
            Tasks.Remove(task);
            SaveTasks();
            RecalculateProjectProgress();
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
        SaveTasks();
        RecalculateProjectProgress();
    }

    private void SaveTasks()
    {
        try
        {
            _storage.Save(Tasks);
        }
        catch
        {
            // If save fails, do not crash the app. Consider showing a notification in future.
        }
    }

    private void ClearNewTaskForm()
    {
        NewTaskTitle = string.Empty;
        NewTaskDescription = string.Empty;
        NewTaskPriority = TaskPriority.Normal;
        NewTaskDueTime = null;
    }

    /// <summary>
    /// Adds a TaskItem to the main task list and persists immediately. This is
    /// used by other sub viewmodels (for example the AI planner) to inject
    /// tasks programmatically while keeping persistence consistent.
    /// </summary>
    public void AddTaskItem(TaskItem task)
    {
        if (task is null) return;
        Tasks.Insert(0, task);
        AttachTaskHandler(task);
        SaveTasks();
        RecalculateProjectProgress();
    }

    private void TasksOnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TaskItem t in e.NewItems)
            {
                AttachTaskHandler(t);
            }
        }

        if (e.OldItems != null)
        {
            foreach (TaskItem t in e.OldItems)
            {
                t.PropertyChanged -= TaskOnPropertyChanged;
            }
        }

        RecalculateProjectProgress();
    }

    private void AttachTaskHandler(TaskItem t)
    {
        if (t == null) return;
        t.PropertyChanged -= TaskOnPropertyChanged;
        t.PropertyChanged += TaskOnPropertyChanged;
    }

    private void TaskOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TaskItem.Status) ||
            e.PropertyName == nameof(TaskItem.IsProject) ||
            e.PropertyName == nameof(TaskItem.ParentId) ||
            e.PropertyName == nameof(TaskItem.Title))
        {
            RecalculateProjectProgress();
        }
    }

    private void RecalculateProjectProgress()
    {
        try
        {
            var groups = Tasks.Where(task => task.IsProject)
                .Select(project =>
                {
                    var projectId = project.Id.ToString();
                    var children = Tasks.Where(task =>
                        !task.IsProject &&
                        string.Equals(task.ParentId, projectId, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    return new
                    {
                        ProjectId = projectId,
                        ProjectName = project.Title,
                        Total = children.Count,
                        Completed = children.Count(task => task.Status == FloatTodo.App.Models.TaskStatus.Done)
                    };
                })
                .ToList();

            // Update ProjectProgressList to match groups
            // Remove entries not present
            var existingIds = ProjectProgressList.Select(p => p.ProjectId).ToList();
            foreach (var id in existingIds)
            {
                if (!groups.Any(g => g.ProjectId == id))
                {
                    var rem = ProjectProgressList.FirstOrDefault(p => p.ProjectId == id);
                    if (rem != null) ProjectProgressList.Remove(rem);
                }
            }

            foreach (var g in groups)
            {
                var item = ProjectProgressList.FirstOrDefault(p => p.ProjectId == g.ProjectId);
                if (item == null)
                {
                    item = new ProjectProgressItem { ProjectId = g.ProjectId, ProjectName = g.ProjectName, Total = g.Total, Completed = g.Completed };
                    ProjectProgressList.Add(item);
                }
                else
                {
                    item.ProjectName = g.ProjectName;
                    item.Total = g.Total;
                    item.Completed = g.Completed;
                }
            }
            OnPropertyChanged(nameof(HasProjectProgress));
        }
        catch
        {
            // Swallow any progress calculation errors to avoid breaking UI.
        }
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
