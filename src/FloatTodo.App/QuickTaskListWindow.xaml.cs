using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
    string Title,
    string Priority,
    string DueTimeDisplay,
    string ProjectName,
    string StatusDisplay,
    string Description)
{
    public string DescriptionPreview => string.IsNullOrWhiteSpace(Description)
        ? "暂无详细内容"
        : Description.Length <= 48 ? Description : Description[..48] + "...";
}

public partial class QuickTaskListWindow : Window
{
    public QuickTaskListWindow(
        string title,
        string emptyText,
        IReadOnlyCollection<QuickTaskListItem> tasks)
    {
        InitializeComponent();
        Title = title;
        EmptyText.Text = emptyText;

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
    /// 只在标题 TextBlock 上绑定，避免影响列表滚动和其他区域点击。
    /// </summary>
    private void TaskTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { DataContext: QuickTaskListItem item })
        {
            return;
        }

        var detail = new QuickTaskDetailWindow(new TaskDetailDisplayItem(
            item.Title,
            item.Priority,
            item.DueTimeDisplay,
            item.ProjectName,
            item.StatusDisplay,
            item.Description))
        {
            Owner = this
        };
        detail.Show();
        e.Handled = true;
    }
}
