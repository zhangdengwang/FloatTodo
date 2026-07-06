using System;
using System.Linq;
using System.Windows;
using FloatTodo.App.Models;
using FloatTodo.App.Services;

namespace FloatTodo.App;

public partial class QuickProjectTasksWindow : Window
{
    private readonly TaskItem _project;

    public QuickProjectTasksWindow(TaskItem project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        InitializeComponent();
        Title = $"项目任务 - {_project.Title}";
        LoadTasks();
    }

    private void LoadTasks()
    {
        var projectId = _project.Id.ToString();
        var tasks = new TaskStorageService().Load()
            .Where(task =>
                !task.IsProject &&
                string.Equals(task.ParentId, projectId, StringComparison.OrdinalIgnoreCase))
            .Select(task => new
            {
                task.Title,
                Priority = task.Priority.ToString(),
                StatusDisplay = task.Status == FloatTodo.App.Models.TaskStatus.Done ? "已完成" : "未完成",
                DueTimeDisplay = task.DueTime.HasValue
                    ? $"截止：{task.DueTime.Value:yyyy-MM-dd HH:mm}"
                    : "截止：未设置"
            })
            .ToList();

        EmptyText.Visibility = tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TasksList.Visibility = tasks.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        TasksList.ItemsSource = tasks;
    }
}
