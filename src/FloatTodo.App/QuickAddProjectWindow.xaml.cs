using System;
using System.Windows;
using FloatTodo.App.Models;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

public partial class QuickAddProjectWindow : Window
{
    public QuickAddProjectWindow()
    {
        InitializeComponent();
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
        if (Application.Current is App app && app.GetMainViewModel() is { } mainViewModel)
        {
            mainViewModel.AddTaskItem(task);
            return;
        }

        new MainViewModel().AddTaskItem(task);
    }
}
