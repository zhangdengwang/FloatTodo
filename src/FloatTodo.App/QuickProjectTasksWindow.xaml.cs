using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FloatTodo.App.Models;
using FloatTodo.App.Services;

namespace FloatTodo.App;

public partial class QuickProjectTasksWindow : Window
{
    private readonly TaskItem _project;
    private readonly Action? _afterTaskChanged;

    public QuickProjectTasksWindow(TaskItem project, Action? afterTaskChanged = null)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _afterTaskChanged = afterTaskChanged;
        InitializeComponent();
        Title = $"项目任务 - {_project.Title}";
        ProjectDescriptionText.Text = string.IsNullOrWhiteSpace(_project.Description)
            ? "项目描述：暂无详细内容"
            : $"项目描述：{_project.Description}";
        LoadTasks();
    }

    private void LoadTasks()
    {
        var now = DateTime.Now;
        var dueSoonLimit = now.AddHours(24);
        var projectId = _project.Id.ToString();
        var tasks = new TaskStorageService().Load()
            .Where(task =>
                !task.IsProject &&
                string.Equals(task.ParentId, projectId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(task => GetDueSortGroup(task.DueTime, now, dueSoonLimit))
            .ThenBy(task => task.DueTime ?? DateTime.MaxValue)
            .ThenBy(task => task.Title, StringComparer.CurrentCulture)
            .Select(task => new ProjectTaskDisplayItem(
                task.Id,
                task.Title,
                task.Priority.ToString(),
                task.Status == FloatTodo.App.Models.TaskStatus.Done ? "已完成" : "未完成",
                task.DueTime,
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
            item.Id,
            item.Title,
            item.Priority,
            item.DueTimeDisplay.Replace("截止：", string.Empty),
            item.ProjectName,
            item.StatusDisplay,
            item.Description),
            () =>
            {
                LoadTasks();
                _afterTaskChanged?.Invoke();
            })
        {
            Owner = this
        };
        detail.Show();
        e.Handled = true;
    }

    private void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProjectTaskDisplayItem item })
        {
            RunTaskOperation(() => new TaskStorageService().MarkCompleted(item.Id), "完成任务失败，请稍后重试。");
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProjectTaskDisplayItem item })
        {
            return;
        }

        var result = MessageBox.Show(this, "确定要删除该任务吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        RunTaskOperation(() => new TaskStorageService().DeleteTask(item.Id), "删除任务失败，请稍后重试。");
    }

    private void RunTaskOperation(Func<bool> operation, string errorMessage)
    {
        try
        {
            if (!operation())
            {
                MessageBox.Show(this, errorMessage, "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadTasks();
            _afterTaskChanged?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"{errorMessage}\n{ex.Message}", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static int GetDueSortGroup(DateTime? dueTime, DateTime now, DateTime dueSoonLimit)
    {
        if (!dueTime.HasValue)
        {
            return 3;
        }

        if (dueTime.Value < now)
        {
            return 0;
        }

        return dueTime.Value <= dueSoonLimit ? 1 : 2;
    }

    private sealed record ProjectTaskDisplayItem(
        Guid Id,
        string Title,
        string Priority,
        string StatusDisplay,
        DateTime? DueTime,
        string DueTimeDisplay,
        string ProjectName,
        string Description)
    {
        public string DescriptionPreview => string.IsNullOrWhiteSpace(Description)
            ? "暂无详细内容"
            : Description.Length <= 48 ? Description : Description[..48] + "...";

        // UrgencyText 根据截止时间实时计算，只用于界面展示；XAML 绑定必须保持 OneWay。
        public string UrgencyText => DueTime switch
        {
            DateTime value when value < DateTime.Now => "已逾期",
            DateTime value when value <= DateTime.Now.AddHours(24) => "24小时内截止",
            DateTime => "未到期",
            _ => "无截止时间"
        };

        // 背景色同样是只读显示属性，按逾期/24小时内/未来/无截止时间区分。
        public Brush Background => DueTime switch
        {
            DateTime value when value < DateTime.Now => new SolidColorBrush(Color.FromRgb(255, 232, 232)),
            DateTime value when value <= DateTime.Now.AddHours(24) => new SolidColorBrush(Color.FromRgb(255, 242, 218)),
            DateTime => new SolidColorBrush(Color.FromRgb(232, 242, 255)),
            _ => new SolidColorBrush(Color.FromRgb(245, 245, 245))
        };
    }
}
