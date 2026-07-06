using System;
using System.Windows;
using FloatTodo.App.Models;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

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

    private void InitializeTimeSelectors()
    {
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
