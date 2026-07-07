using System;
using System.Windows;
using FloatTodo.App.Models;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

/// <summary>
/// 右键菜单中的“新建项目”窗口。
/// 项目本身保存为 IsProject=true 的任务父节点，后续小任务通过 ParentId 关联到它。
/// </summary>
public partial class QuickAddProjectWindow : Window
{
    public QuickAddProjectWindow()
    {
        InitializeComponent();
        // 项目默认选择今天作为截止日期，演示和日常使用时少点一步；清空后仍按无截止时间保存。
        DueDatePicker.SelectedDate = DateTime.Today;
        InitializeTimeSelectors();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        var projectName = ProjectNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(projectName))
        {
            MessageBox.Show(this, "请输入项目名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryGetDueTime(out var dueTime))
        {
            return;
        }

        var project = new TaskItem
        {
            Title = projectName,
            Description = DescriptionTextBox.Text.Trim(),
            DueTime = dueTime,
            Priority = TaskPriority.Important,
            Status = FloatTodo.App.Models.TaskStatus.Todo,
            IsProject = true,
            ParentId = null,
            ProjectId = string.Empty,
            ProjectName = projectName
        };

        AddTask(project);
        Close();
    }

    private bool TryGetDueTime(out DateTime? dueTime)
    {
        // 项目截止时间同样允许为空。
        // 选了日期但不选小时则默认 23:59；选了小时但不选分钟则默认 00。
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

    private void ClearDueTimeButton_Click(object sender, RoutedEventArgs e)
    {
        DueDatePicker.SelectedDate = null;
        DueHourComboBox.SelectedItem = null;
        DueMinuteComboBox.SelectedItem = null;
    }

    private void InitializeTimeSelectors()
    {
        // 使用下拉框选择小时/分钟，降低课程演示时输入格式错误的概率。
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
        // 优先写入已打开主面板的任务集合；没有主面板时仍通过 MainViewModel 保存到同一个 tasks.json。
        if (Application.Current is App app && app.GetMainViewModel() is { } mainViewModel)
        {
            mainViewModel.AddTaskItem(task);
            return;
        }

        new MainViewModel().AddTaskItem(task);
    }
}
