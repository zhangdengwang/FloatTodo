using System;
using System.Linq;
using System.Windows;
using FloatTodo.App.Services;

namespace FloatTodo.App;

public partial class QuickDailyRecordsWindow : Window
{
    public QuickDailyRecordsWindow()
    {
        InitializeComponent();
        RefreshRecords();
    }

    public void RefreshRecords()
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
        RecordsList.ItemsSource = records.Select(record => new
        {
            record.Name,
            TodayCount = record.LastRecordTime?.Date == DateTime.Today ? record.TodayCount : 0,
            LastRecordTimeDisplay = record.LastRecordTime?.ToString("yyyy-MM-dd HH:mm") ?? "未记录"
        }).ToList();
    }
}
