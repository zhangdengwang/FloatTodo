using System;
using System.Linq;
using System.Windows;
using FloatTodo.App.Services;

namespace FloatTodo.App;

/// <summary>
/// 日常记录只读查看窗口。
/// 打开或刷新时一次性读取现有 daily-records.json，不在窗口内部循环读取，避免 UI 卡顿。
/// </summary>
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
            LastRecordTimeDisplay = record.LastRecordTime?.ToString("yyyy-MM-dd HH:mm") ?? "未记录",
            ThresholdDisplay = record.ReminderThresholdMinutes is > 0
                ? $"{record.ReminderThresholdMinutes} 分钟"
                : "未启用"
        }).ToList();
    }
}
