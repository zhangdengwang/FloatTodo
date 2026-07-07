using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FloatTodo.App.Services;

namespace FloatTodo.App;

public enum QuickTaskFilter
{
    Unfinished,
    DueSoon
}

/// <summary>
/// 快捷任务列表中的展示项。
/// 除了列表上直接显示的信息，也保留 Description 等详情字段供点击标题时打开详情窗口。
/// </summary>
public sealed record QuickTaskListItem(
    Guid Id,
    string Title,
    string Priority,
    DateTime? DueTime,
    string DueTimeDisplay,
    string ProjectName,
    string StatusDisplay,
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

public partial class QuickTaskListWindow : Window
{
    private readonly Func<IReadOnlyCollection<QuickTaskListItem>> _reloadItems;
    private readonly Action? _afterTaskChanged;

    public QuickTaskListWindow(
        string title,
        string emptyText,
        Func<IReadOnlyCollection<QuickTaskListItem>> reloadItems,
        Action? afterTaskChanged = null)
    {
        InitializeComponent();
        Title = title;
        EmptyText.Text = emptyText;
        _reloadItems = reloadItems;
        _afterTaskChanged = afterTaskChanged;
        RefreshTasks();
    }

    /// <summary>
    /// 重新加载当前列表。
    /// 完成或删除任务后立即刷新列表，确保快截止列表和红点状态同步变化。
    /// </summary>
    public void RefreshTasks()
    {
        var tasks = _reloadItems();
        if (tasks.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            TasksList.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyText.Visibility = Visibility.Collapsed;
            TasksList.Visibility = Visibility.Visible;
            TasksList.ItemsSource = tasks;
        }
    }

    /// <summary>
    /// 点击任务标题查看详情。
    /// 只在标题 TextBlock 上绑定，避免影响列表滚动和完成/删除按钮点击。
    /// </summary>
    private void TaskTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { DataContext: QuickTaskListItem item })
        {
            return;
        }

        var detail = new QuickTaskDetailWindow(new TaskDetailDisplayItem(
            item.Id,
            item.Title,
            item.Priority,
            item.DueTimeDisplay,
            item.ProjectName,
            item.StatusDisplay,
            item.Description),
            () =>
            {
                RefreshTasks();
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
        if (sender is Button { Tag: QuickTaskListItem item })
        {
            RunTaskOperation(() => new TaskStorageService().MarkCompleted(item.Id), "完成任务失败，请稍后重试。");
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: QuickTaskListItem item })
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

            RefreshTasks();
            _afterTaskChanged?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"{errorMessage}\n{ex.Message}", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
