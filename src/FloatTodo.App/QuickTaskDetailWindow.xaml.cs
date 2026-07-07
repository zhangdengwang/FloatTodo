using System;
using System.Windows;
using FloatTodo.App.Services;

namespace FloatTodo.App;

/// <summary>
/// 任务详情窗口。
/// 用于从快捷任务列表、项目任务列表或截止提醒中查看 Description；普通任务和项目小任务可在这里完成/删除。
/// </summary>
public partial class QuickTaskDetailWindow : Window
{
    private readonly Guid? _taskId;
    private readonly Action? _afterTaskChanged;

    public QuickTaskDetailWindow(TaskDetailDisplayItem item, Action? afterTaskChanged = null)
    {
        InitializeComponent();
        _taskId = item.TaskId;
        _afterTaskChanged = afterTaskChanged;

        TitleText.Text = item.Title;
        PriorityText.Text = $"优先级：{item.Priority}";
        DueTimeText.Text = $"截止时间：{item.DueTimeDisplay}";
        ProjectText.Text = string.IsNullOrWhiteSpace(item.ProjectName)
            ? "所属项目：无"
            : $"所属项目：{item.ProjectName}";
        StatusText.Text = $"完成状态：{item.StatusDisplay}";
        DescriptionText.Text = string.IsNullOrWhiteSpace(item.Description)
            ? "暂无详细内容"
            : item.Description;

        // 项目父节点只作为组织小任务的容器，不在详情窗口中提供完成/删除，避免误删整个项目。
        var canOperate = item.TaskId.HasValue;
        CompleteButton.Visibility = canOperate && item.StatusDisplay != "已完成"
            ? Visibility.Visible
            : Visibility.Collapsed;
        DeleteButton.Visibility = canOperate ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 在详情窗口中完成当前任务。
    /// 操作仍通过 TaskStorageService 保存，成功后通知打开方刷新红点、截止提醒和列表。
    /// </summary>
    private void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        RunTaskOperation(id => new TaskStorageService().MarkCompleted(id), "完成任务失败，请稍后重试。", confirmBeforeDelete: false);
    }

    /// <summary>
    /// 在详情窗口中删除当前任务。
    /// 删除前需要用户确认；本方法只删除普通任务或项目小任务，不删除项目父节点。
    /// </summary>
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        RunTaskOperation(id => new TaskStorageService().DeleteTask(id), "删除任务失败，请稍后重试。", confirmBeforeDelete: true);
    }

    private void RunTaskOperation(Func<Guid, bool> operation, string errorMessage, bool confirmBeforeDelete)
    {
        if (!_taskId.HasValue)
        {
            return;
        }

        if (confirmBeforeDelete)
        {
            var result = MessageBox.Show(this, "确定要删除该任务吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            if (!operation(_taskId.Value))
            {
                MessageBox.Show(this, errorMessage, "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _afterTaskChanged?.Invoke();
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"{errorMessage}\n{ex.Message}", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// 任务详情窗口使用的轻量展示数据。
/// 与 TaskItem 分开是为了让不同列表窗口可以传入已经格式化好的时间、状态和项目名称。
/// TaskId 为空表示项目父节点等只读详情；有值时详情窗口可以复用现有任务服务完成或删除任务。
/// </summary>
public sealed record TaskDetailDisplayItem(
    Guid? TaskId,
    string Title,
    string Priority,
    string DueTimeDisplay,
    string ProjectName,
    string StatusDisplay,
    string Description);
