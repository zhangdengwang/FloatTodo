using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using FloatTodo.App.Models;
using FloatTodo.App.Services;

namespace FloatTodo.App;

public enum QuickTaskFilter
{
    Unfinished,
    DueSoon
}

public partial class QuickTaskListWindow : Window
{
    public QuickTaskListWindow(QuickTaskFilter filter)
    {
        InitializeComponent();
        LoadTasks(filter);
    }

    private void LoadTasks(QuickTaskFilter filter)
    {
        var storage = new TaskStorageService();
        var items = storage.Load() ?? new List<TaskItem>();

        IEnumerable<object> viewItems = Enumerable.Empty<object>();

        if (filter == QuickTaskFilter.Unfinished)
        {
            var q = items.Where(t => t.Status != FloatTodo.App.Models.TaskStatus.Done)
                         .Select(t => new { t.Title, Priority = t.Priority.ToString(), DueTimeDisplay = t.DueTime.HasValue ? t.DueTime.Value.ToString("yyyy-MM-dd HH:mm") : "无截止时间" })
                         .ToList();
            viewItems = q;
        }
        else if (filter == QuickTaskFilter.DueSoon)
        {
            var now = DateTime.Now;
            var limit = now.AddHours(24);
            var q = items.Where(t => t.Status != FloatTodo.App.Models.TaskStatus.Done && t.DueTime.HasValue && t.DueTime.Value <= limit)
                         .Select(t => new { t.Title, Priority = t.Priority.ToString(), DueTime = t.DueTime.Value, DueTimeDisplay = t.DueTime.Value.ToString("yyyy-MM-dd HH:mm"), IsOverdue = t.DueTime.Value < now })
                         .Select(t => new { Title = t.Title + (t.IsOverdue ? "  (已逾期)" : "  (即将截止)"), Priority = t.Priority, DueTimeDisplay = t.DueTimeDisplay })
                         .ToList();
            viewItems = q;
        }

        var list = viewItems.ToList();
        if (list.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            TasksList.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyText.Visibility = Visibility.Collapsed;
            TasksList.Visibility = Visibility.Visible;
            TasksList.ItemsSource = list;
        }
    }
}
