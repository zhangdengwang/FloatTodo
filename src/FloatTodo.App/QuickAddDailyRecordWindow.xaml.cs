using System.Windows;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

/// <summary>
/// 日常记录新增/修改窗口。
/// 记录不存在时创建新项；记录已存在时只更新提醒阈值，避免影响已有次数和最后记录时间。
/// </summary>
public partial class QuickAddDailyRecordWindow : Window
{
    public QuickAddDailyRecordWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "请输入记录项名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryReadReminderThreshold(out var reminderThresholdMinutes))
        {
            return;
        }

        var recordsViewModel = GetDailyRecordsViewModel();
        if (recordsViewModel.AddRecordByName(name, string.Empty, string.Empty, reminderThresholdMinutes))
        {
            Close();
            return;
        }

        if (!recordsViewModel.UpdateRecordReminderThresholdByName(name, reminderThresholdMinutes))
        {
            MessageBox.Show(this, "保存日常记录失败，请稍后重试。", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Close();
    }

    /// <summary>
    /// 读取提醒阈值。
    /// 留空表示不启用提醒；输入正整数时按分钟保存。
    /// </summary>
    private bool TryReadReminderThreshold(out int? reminderThresholdMinutes)
    {
        reminderThresholdMinutes = null;
        var thresholdText = ReminderThresholdTextBox.Text.Trim();
        if (string.IsNullOrEmpty(thresholdText))
        {
            return true;
        }

        if (!int.TryParse(thresholdText, out var parsedThreshold) || parsedThreshold <= 0)
        {
            MessageBox.Show(this, "提醒阈值必须是正整数", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        reminderThresholdMinutes = parsedThreshold;
        return true;
    }

    /// <summary>
    /// 优先复用完整主面板中的日常记录 ViewModel。
    /// 如果主面板未打开，则创建临时 ViewModel，并继续使用同一个 daily-records.json。
    /// </summary>
    private static DailyRecordsViewModel GetDailyRecordsViewModel()
    {
        if (Application.Current is App app && app.GetMainViewModel() is { } mainVm)
        {
            return mainVm.DailyRecords;
        }

        return new DailyRecordsViewModel();
    }
}
