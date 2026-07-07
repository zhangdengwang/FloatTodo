using System;
using System.Windows;
using FloatTodo.App.Models;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

/// <summary>
/// 新增项目小任务窗口。
/// 小任务本质仍是普通 TaskItem，只是通过 ParentId 指向所属项目父节点。
/// </summary>
public partial class QuickAddSubTaskWindow : Window
{
    // 当前要添加小任务的项目父节点。
    private readonly TaskItem _project;

    public QuickAddSubTaskWindow(TaskItem project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        InitializeComponent();
        InitializeTimeSelectors();
        Title = $"新增小任务 - {_project.Title}";
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show(this, "请输入小任务标题", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryGetDueTime(out var dueTime))
        {
            return;
        }

        var priority = PriorityComboBox.SelectedIndex switch
        {
            1 => TaskPriority.Important,
            2 => TaskPriority.Urgent,
            _ => TaskPriority.Normal
        };
        // ParentId 使用项目父节点 Id，项目进度统计会根据这个关系寻找所有子任务。
        var projectId = _project.Id.ToString();
        var task = new TaskItem
        {
            Title = title,
            Description = DescriptionTextBox.Text.Trim(),
            Priority = priority,
            DueTime = dueTime,
            Status = FloatTodo.App.Models.TaskStatus.Todo,
            IsProject = false,
            ParentId = projectId,
            ProjectId = projectId,
            ProjectName = _project.Title
        };

        AddTask(task);
        Close();
    }

    private bool TryGetDueTime(out DateTime? dueTime)
    {
        // 小任务可以没有截止时间；有日期时再组合小时和分钟。
        // 这种规则和普通任务、项目窗口保持一致。
        dueTime = null;
        if (DueDatePicker.SelectedDate is not DateTime selectedDate)
        {
            return true;
        }

        if (DueHourComboBox.SelectedItem is string hourText)
        {
            var hour = int.Parse(hourText);
            var minute = DueMinuteComboBox.SelectedItem is string minuteText
                ? int.Parse(minuteText)
                : 0;
            dueTime = selectedDate.Date.AddHours(hour).AddMinutes(minute);
        }
        else
        {
            dueTime = selectedDate.Date.AddHours(23).AddMinutes(59);
        }

        return true;
    }

    private void InitializeTimeSelectors()
    {
        // 小时固定 00-23，分钟固定 00/15/30/45，方便快速选择。
        for (var hour = 0; hour < 24; hour++)
        {
            DueHourComboBox.Items.Add(hour.ToString("00"));
        }

        foreach (var minute in new[] { "00", "15", "30", "45" })
        {
            DueMinuteComboBox.Items.Add(minute);
        }
    }

    private static void AddTask(TaskItem task)
    {
        // 小任务也走统一的 MainViewModel.AddTaskItem，确保保存、进度刷新和 JSON 格式一致。
        if (Application.Current is App app && app.GetMainViewModel() is { } mainViewModel)
        {
            mainViewModel.AddTaskItem(task);
            return;
        }

        new MainViewModel().AddTaskItem(task);
    }
}
