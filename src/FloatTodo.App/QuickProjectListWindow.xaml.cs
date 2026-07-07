using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FloatTodo.App.Models;
using FloatTodo.App.Services;

namespace FloatTodo.App;

public partial class QuickProjectListWindow : Window
{
    public QuickProjectListWindow()
    {
        InitializeComponent();
        RefreshProjects();
    }

    public void RefreshProjects()
    {
        var tasks = new TaskStorageService().Load();
        var projects = tasks
            .Where(task => task.IsProject)
            .Select(project =>
            {
                var projectId = project.Id.ToString();
                var children = tasks
                    .Where(task =>
                        !task.IsProject &&
                        string.Equals(task.ParentId, projectId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var completed = children.Count(task => task.Status == FloatTodo.App.Models.TaskStatus.Done);
                var percentage = children.Count == 0
                    ? 0
                    : (int)Math.Round((double)completed / children.Count * 100);

                return new ProjectListDisplayItem
                {
                    Project = project,
                    DueTimeDisplay = project.DueTime.HasValue
                        ? $"截止：{project.DueTime.Value:yyyy-MM-dd HH:mm}"
                        : "截止：未设置",
                    ProgressDisplay = $"子任务：{completed} / {children.Count}，进度 {percentage}%",
                    StatusDisplay = children.Count > 0 && completed == children.Count
                        ? "项目状态：已完成"
                        : "项目状态：未完成"
                };
            })
            .ToList();

        EmptyText.Visibility = projects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ProjectsList.Visibility = projects.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        ProjectsList.ItemsSource = projects;
    }

    private void ViewTasksButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TaskItem project })
        {
            return;
        }

        var window = new QuickProjectTasksWindow(project, RefreshAfterProjectTaskChanged)
        {
            Owner = this
        };
        window.Show();
    }

    private void RefreshAfterProjectTaskChanged()
    {
        RefreshProjects();

        if (Owner is MiniWidgetWindow miniWidget)
        {
            miniWidget.RefreshPetState();
        }
    }

    /// <summary>
    /// 点击项目名称查看项目详情。
    /// 项目本身仍是 IsProject=true 的 TaskItem，详情窗口只读展示项目 Description。
    /// </summary>
    private void ProjectTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { DataContext: ProjectListDisplayItem { Project: var project } })
        {
            return;
        }

        var detail = new QuickTaskDetailWindow(new TaskDetailDisplayItem(
            project.Title,
            project.Priority.ToString(),
            project.DueTime.HasValue ? project.DueTime.Value.ToString("yyyy-MM-dd HH:mm") : "无截止时间",
            string.Empty,
            project.Status == FloatTodo.App.Models.TaskStatus.Done ? "已完成" : "未完成",
            project.Description))
        {
            Owner = this
        };
        detail.Show();
        e.Handled = true;
    }

    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TaskItem project })
        {
            return;
        }

        var window = new QuickAddSubTaskWindow(project)
        {
            Owner = this
        };
        window.ShowDialog();
        RefreshProjects();

        if (Owner is MiniWidgetWindow miniWidget)
        {
            miniWidget.RefreshPetState();
        }
    }

    private sealed class ProjectListDisplayItem
    {
        public required TaskItem Project { get; init; }
        public required string DueTimeDisplay { get; init; }
        public required string ProgressDisplay { get; init; }
        public required string StatusDisplay { get; init; }
    }
}
