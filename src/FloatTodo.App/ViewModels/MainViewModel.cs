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
/// 主面板 ViewModel。
/// 负责维护任务集合、项目进度、日常记录和 AI 设置，并统一调用 TaskStorageService 保存任务 JSON。
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

        // 任务集合变化时重新计算项目进度，保证新增/删除项目小任务后展示同步更新。
        Tasks.CollectionChanged += TasksOnCollectionChanged;

        // 启动时从 tasks.json 读取历史任务。
        // 读取失败会回退为空列表，避免本地数据损坏导致主界面无法打开。
        try
        {
            var items = _storage.Load();
            foreach (var t in items)
            {
                Tasks.Add(t);
            }
            // 已加载任务也要挂 PropertyChanged，后续状态变化才能触发项目进度刷新。
            foreach (var t in Tasks)
            {
                AttachTaskHandler(t);
            }
        }
        catch
        {
            // Swallow any load errors to keep app running with empty tasks.
        }
        // 日常记录、API 设置和 AI 拆解各自维护独立 ViewModel，主 ViewModel 只负责组合它们。
        DailyRecords = new DailyRecordsViewModel();
        AiSettings = new ApiSettingsViewModel(_apiSettingsService);
        AiPlanner = new AiPlannerViewModel(this, new AiPlannerService(_apiSettingsService));

        // 首次加载后计算一次项目进度，避免界面初始为空。
        RecalculateProjectProgress();
    }

    public ObservableCollection<ProjectProgressItem> ProjectProgressList { get; }

    public bool HasProjectProgress => ProjectProgressList.Any();

    public ObservableCollection<TaskItem> Tasks { get; }

    // 暴露日常记录 ViewModel，供 ShellView 和桌宠复用同一份内存状态。
    public DailyRecordsViewModel DailyRecords { get; }

    public ApiSettingsViewModel AiSettings { get; }

    public TaskPriority[] Priorities { get; } = Enum.GetValues<TaskPriority>();

    // 暴露 AI 拆解 ViewModel，完整面板和快捷窗口都能使用同一套拆解逻辑。
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

        // 完整主面板里的普通任务新增入口。
        // 这里创建的是非项目任务，因此 IsProject=false、ParentId=null。
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

        // 标记完成时记录 CompletedAt，既方便界面展示，也为后续统计留出数据。
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
            // 保存失败不让应用崩溃；当前版本以轻量演示为主，后续可替换成非阻塞通知。
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
    /// 从快捷窗口或 AI 拆解等外部入口加入任务，并立即保存。
    /// 所有入口最终都走这里，保证任务列表、项目进度和 JSON 存储保持一致。
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
            // 项目进度不单独存储，而是扫描所有 IsProject=true 的父节点，
            // 再统计 ParentId 指向它的子任务数量和完成数量。
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

            // 先移除已经不存在的项目进度项，避免删除项目后进度列表残留旧数据。
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
                // 已存在的进度项只更新数值；新项目则新增一条展示项。
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
            // 进度是辅助展示，计算失败不应影响任务主流程。
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
