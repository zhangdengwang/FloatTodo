using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        ProjectDescriptionText.Text = string.IsNullOrWhiteSpace(_project.Description)
            ? "项目描述：暂无详细内容"
            : $"项目描述：{_project.Description}";
        LoadTasks();
    }

    private void LoadTasks()
    {
        var projectId = _project.Id.ToString();
        var tasks = new TaskStorageService().Load()
            .Where(task =>
                !task.IsProject &&
                string.Equals(task.ParentId, projectId, StringComparison.OrdinalIgnoreCase))
            .Select(task => new ProjectTaskDisplayItem(
                task.Title,
                task.Priority.ToString(),
                task.Status == FloatTodo.App.Models.TaskStatus.Done ? "已完成" : "未完成",
                task.DueTime.HasValue
                    ? $"截止：{task.DueTime.Value:yyyy-MM-dd HH:mm}"
                    : "截止：未设置",
                task.ProjectName,
                task.Description))
            .ToList();

        EmptyText.Visibility = tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TasksList.Visibility = tasks.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        TasksList.ItemsSource = tasks;
    }

    /// <summary>
    /// 点击项目小任务标题查看详情。
    /// 只读展示 Description，不影响项目进度统计和任务筛选规则。
    /// </summary>
    private void TaskTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { DataContext: ProjectTaskDisplayItem item })
        {
            return;
        }

        var detail = new QuickTaskDetailWindow(new TaskDetailDisplayItem(
            item.Title,
            item.Priority,
            item.DueTimeDisplay.Replace("截止：", string.Empty),
            item.ProjectName,
            item.StatusDisplay,
            item.Description))
        {
            Owner = this
        };
        detail.Show();
        e.Handled = true;
    }

    private sealed record ProjectTaskDisplayItem(
        string Title,
        string Priority,
        string StatusDisplay,
        string DueTimeDisplay,
        string ProjectName,
        string Description)
    {
        public string DescriptionPreview => string.IsNullOrWhiteSpace(Description)
            ? "暂无详细内容"
            : Description.Length <= 48 ? Description : Description[..48] + "...";
    }
}
