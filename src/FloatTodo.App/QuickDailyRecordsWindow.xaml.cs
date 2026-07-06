using System.Windows;
using FloatTodo.App.Services;

namespace FloatTodo.App;

public partial class QuickDailyRecordsWindow : Window
{
    public QuickDailyRecordsWindow()
    {
        InitializeComponent();
        LoadRecords();
    }

    private void LoadRecords()
    {
        var records = new DailyRecordStorageService().Load();
        if (records.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            RecordsList.Visibility = Visibility.Collapsed;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;
        RecordsList.Visibility = Visibility.Visible;
        RecordsList.ItemsSource = records;
    }
}
