using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FloatTodo.App.Services;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

/// <summary>
/// 日常记录管理窗口。
/// 打开时一次性读取现有记录，可修改提醒阈值；保存仍复用 DailyRecordsViewModel 和现有 JSON。
/// </summary>
public partial class QuickDailyRecordsWindow : Window
{
    private readonly ObservableCollection<DailyRecordEditItem> _records = new();

    public QuickDailyRecordsWindow()
    {
        InitializeComponent();
        RefreshRecords();
    }

    public void RefreshRecords()
    {
        _records.Clear();
        var records = new DailyRecordStorageService().Load();
        foreach (var record in records)
        {
            _records.Add(new DailyRecordEditItem
            {
                Name = record.Name,
                TodayCount = record.LastRecordTime?.Date == DateTime.Today ? record.TodayCount : 0,
                LastRecordTimeDisplay = record.LastRecordTime?.ToString("yyyy-MM-dd HH:mm") ?? "未记录",
                ThresholdText = record.ReminderThresholdMinutes?.ToString() ?? string.Empty
            });
        }

        EmptyText.Visibility = _records.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RecordsList.Visibility = _records.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        RecordsList.ItemsSource = _records;
    }

    private void SaveThresholdButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DailyRecordEditItem item })
        {
            return;
        }

        if (!TryParseThreshold(item.ThresholdText, out var threshold))
        {
            MessageBox.Show(this, "提醒阈值必须是正整数，或留空表示不提醒。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var viewModel = GetDailyRecordsViewModel();
        if (!viewModel.UpdateRecordReminderThresholdByName(item.Name, threshold))
        {
            MessageBox.Show(this, "提醒阈值保存失败，请稍后重试。", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        RefreshRecords();
    }

    private static bool TryParseThreshold(string? text, out int? threshold)
    {
        threshold = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!int.TryParse(text.Trim(), out var value) || value <= 0)
        {
            return false;
        }

        threshold = value;
        return true;
    }

    private static DailyRecordsViewModel GetDailyRecordsViewModel()
    {
        return Application.Current is App app && app.GetMainViewModel() is { } mainVm
            ? mainVm.DailyRecords
            : new DailyRecordsViewModel();
    }

    private sealed class DailyRecordEditItem
    {
        public string Name { get; init; } = string.Empty;
        public int TodayCount { get; init; }
        public string LastRecordTimeDisplay { get; init; } = string.Empty;
        public string ThresholdText { get; set; } = string.Empty;
    }
}
