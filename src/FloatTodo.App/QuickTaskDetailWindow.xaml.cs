using System.Windows;

namespace FloatTodo.App;

/// <summary>
/// 任务详情只读窗口。
/// 用于从快捷任务列表或项目任务列表中查看 Description，不提供编辑，避免影响现有保存逻辑。
/// </summary>
public partial class QuickTaskDetailWindow : Window
{
    public QuickTaskDetailWindow(TaskDetailDisplayItem item)
    {
        InitializeComponent();

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
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// 任务详情窗口使用的轻量展示数据。
/// 与 TaskItem 分开是为了让不同列表窗口可以传入已经格式化好的时间、状态和项目名称。
/// </summary>
public sealed record TaskDetailDisplayItem(
    string Title,
    string Priority,
    string DueTimeDisplay,
    string ProjectName,
    string StatusDisplay,
    string Description);
