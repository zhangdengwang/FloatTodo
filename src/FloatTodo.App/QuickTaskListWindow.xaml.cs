using System.Collections.Generic;
using System.Windows;

namespace FloatTodo.App;

public enum QuickTaskFilter
{
    Unfinished,
    DueSoon
}

public sealed record QuickTaskListItem(
    string Title,
    string Priority,
    string DueTimeDisplay);

public partial class QuickTaskListWindow : Window
{
    public QuickTaskListWindow(
        string title,
        string emptyText,
        IReadOnlyCollection<QuickTaskListItem> tasks)
    {
        InitializeComponent();
        Title = title;
        EmptyText.Text = emptyText;

        if (tasks.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            TasksList.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyText.Visibility = Visibility.Collapsed;
            TasksList.Visibility = Visibility.Visible;
            TasksList.ItemsSource = tasks;
        }
    }
}
