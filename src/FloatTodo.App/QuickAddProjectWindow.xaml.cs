using System;
using System.Globalization;
using System.Windows;
using FloatTodo.App.Models;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

public partial class QuickAddProjectWindow : Window
{
    public QuickAddProjectWindow()
    {
        InitializeComponent();
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
        dueTime = null;
        if (DueDatePicker.SelectedDate is not DateTime selectedDate)
        {
            return true;
        }

        var selectedTime = new TimeSpan(23, 59, 0);
        var timeText = DueTimeTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(timeText))
        {
            if (!DateTime.TryParseExact(
                    timeText,
                    "HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedTime))
            {
                MessageBox.Show(this, "时间格式应为 HH:mm", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            selectedTime = parsedTime.TimeOfDay;
        }

        dueTime = selectedDate.Date.Add(selectedTime);
        return true;
    }

    private static void AddTask(TaskItem task)
    {
        if (Application.Current is App app && app.GetMainViewModel() is { } mainViewModel)
        {
            mainViewModel.AddTaskItem(task);
            return;
        }

        new MainViewModel().AddTaskItem(task);
    }
}
