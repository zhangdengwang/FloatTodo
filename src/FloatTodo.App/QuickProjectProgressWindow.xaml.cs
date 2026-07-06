using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using FloatTodo.App.Models;
using FloatTodo.App.Services;

namespace FloatTodo.App;

public partial class QuickProjectProgressWindow : Window
{
    public QuickProjectProgressWindow()
    {
        InitializeComponent();
        LoadProjectProgress();
    }

    private void LoadProjectProgress()
    {
        var storage = new TaskStorageService();
        var items = storage.Load() ?? new List<TaskItem>();

        var groups = items.Where(t => !string.IsNullOrWhiteSpace(t.ProjectName))
                          .GroupBy(t => t.ProjectName)
                          .Select(g => new
                          {
                              ProjectName = g.Key,
                              Total = g.Count(),
                              Completed = g.Count(t => t.Status == FloatTodo.App.Models.TaskStatus.Done),
                              Percent = Math.Round(100.0 * g.Count(t => t.Status == FloatTodo.App.Models.TaskStatus.Done) / Math.Max(1, g.Count()), 1) + "%"
                          })
                          .ToList();

        if (groups.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            ProjectsList.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyText.Visibility = Visibility.Collapsed;
            ProjectsList.Visibility = Visibility.Visible;
            ProjectsList.ItemsSource = groups;
        }
    }
}
