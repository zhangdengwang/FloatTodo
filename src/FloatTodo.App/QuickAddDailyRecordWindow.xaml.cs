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

        if (!recordsViewModel.AddRecordByName(name, string.Empty, string.Empty))
        {
            MessageBox.Show(this, "记录项已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Close();
    }
}
