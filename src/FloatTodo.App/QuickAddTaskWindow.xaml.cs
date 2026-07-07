using System;
using System.Windows;
using FloatTodo.App.Models;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

/// <summary>
/// 右键菜单中的“快速新增普通任务”窗口。
/// 只负责收集任务标题、优先级和可选截止时间，保存仍复用 MainViewModel/TaskStorageService。
/// </summary>
public partial class QuickAddTaskWindow : Window
{
    public QuickAddTaskWindow()
    {
        InitializeComponent();
        InitializeTimeSelectors();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show(this, "请输入任务标题", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var priority = TaskPriority.Normal;
        switch (PriorityComboBox.SelectedIndex)
        {
            case 1:
                priority = TaskPriority.Important;
                break;
            case 2:
                priority = TaskPriority.Urgent;
                break;
        }

        // 截止日期为空时表示用户不需要提醒，因此 DueTime 保存为 null。
        // 如果选择了日期但没选小时，默认当天 23:59，减少用户必须填写时间的负担。
        DateTime? dueTime = null;
        if (DueDatePicker.SelectedDate is DateTime selectedDate)
        {
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
        }

        var task = new TaskItem
        {
            Title = title,
            Description = DescriptionTextBox.Text.Trim(),
            Priority = priority,
            DueTime = dueTime,
            Status = FloatTodo.App.Models.TaskStatus.Todo,
            IsProject = false,
            ParentId = null
        };

        if (Application.Current is App app)
        {
            var mainViewModel = app.GetMainViewModel();
            if (mainViewModel != null)
            {
                mainViewModel.AddTaskItem(task);
                Close();
                return;
            }
        }

        // 如果完整主面板尚未创建，通过临时 MainViewModel 复用同一套保存逻辑。
        // 这样右键快捷入口不会新增第二套 JSON 存储。
        var backupViewModel = new MainViewModel();
        backupViewModel.AddTaskItem(task);
        Close();
    }

    private void InitializeTimeSelectors()
    {
        // 小时和分钟使用下拉框，避免让用户手输 HH:mm 造成格式错误。
        for (var hour = 0; hour < 24; hour++)
        {
            DueHourComboBox.Items.Add(hour.ToString("00"));
        }

        foreach (var minute in new[] { "00", "15", "30", "45" })
        {
            DueMinuteComboBox.Items.Add(minute);
        }
    }
}
