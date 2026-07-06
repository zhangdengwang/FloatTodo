using System;
using System.Globalization;
using System.Windows;
using FloatTodo.App.Models;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

public partial class QuickAddSubTaskWindow : Window
{
    private readonly TaskItem _project;

    public QuickAddSubTaskWindow(TaskItem project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        InitializeComponent();
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
        var projectId = _project.Id.ToString();
        var task = new TaskItem
        {
            Title = title,
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
