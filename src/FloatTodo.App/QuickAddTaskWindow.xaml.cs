using System;
using System.Globalization;
using System.Windows;
using FloatTodo.App.Models;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

public partial class QuickAddTaskWindow : Window
{
    public QuickAddTaskWindow()
    {
        InitializeComponent();
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

        DateTime? dueTime = null;
        if (DueDatePicker.SelectedDate is DateTime selectedDate)
        {
            var selectedTime = new TimeSpan(23, 59, 0);
            var dueTimeText = DueTimeTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(dueTimeText))
            {
                if (!DateTime.TryParseExact(
                        dueTimeText,
                        "HH:mm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedTime))
                {
                    MessageBox.Show(this, "时间格式应为 HH:mm", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                selectedTime = parsedTime.TimeOfDay;
            }

            dueTime = selectedDate.Date.Add(selectedTime);
        }

        var task = new TaskItem
        {
            Title = title,
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

        // If main view model is not available, persist through a temporary view model so storage is still reused.
        var backupViewModel = new MainViewModel();
        backupViewModel.AddTaskItem(task);
        Close();
    }
}
