using System.Windows;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

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

        int? reminderThresholdMinutes = null;
        var thresholdText = ReminderThresholdTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(thresholdText))
        {
            if (!int.TryParse(thresholdText, out var parsedThreshold) || parsedThreshold <= 0)
            {
                MessageBox.Show(this, "提醒阈值必须是正整数", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            reminderThresholdMinutes = parsedThreshold;
        }

        DailyRecordsViewModel recordsViewModel;
        if (Application.Current is App app)
        {
            var mainVm = app.GetMainViewModel();
            if (mainVm != null)
            {
                recordsViewModel = mainVm.DailyRecords;
            }
            else
            {
                recordsViewModel = new DailyRecordsViewModel();
            }
        }
        else
        {
            recordsViewModel = new DailyRecordsViewModel();
        }

        if (!recordsViewModel.AddRecordByName(
                name,
                string.Empty,
                string.Empty,
                reminderThresholdMinutes))
        {
            MessageBox.Show(this, "记录项已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Close();
    }
}
