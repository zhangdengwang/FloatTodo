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
        try
        {
            LoadTasks(filter);
        }
        catch (Exception ex)
        {
            LogStartupError(ex);
            MessageBox.Show(this, "加载任务失败，请检查日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            var dueSoonItems = new List<object>();

            foreach (var task in items)
            {
                if (task.Status == FloatTodo.App.Models.TaskStatus.Done || !task.DueTime.HasValue)
                {
                    continue;
                }

                var dueTime = task.DueTime.Value;
                if (dueTime > limit)
                {
                    continue;
                }

                dueSoonItems.Add(new
                {
                    Title = task.Title + (dueTime < now ? "  (已逾期)" : "  (即将截止)"),
                    Priority = task.Priority.ToString(),
                    DueTimeDisplay = dueTime.ToString("yyyy-MM-dd HH:mm")
                });
            }

            viewItems = dueSoonItems;
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

    private static void LogStartupError(Exception exception)
    {
        try
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "startup.log");
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] QuickTaskListWindow error: {exception}\r\n";
            System.IO.File.AppendAllText(path, message);
        }
        catch
        {
            // ignore logging failures
        }
    }
}
