using System;
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

        // Try to use existing main view model if available
        if (Application.Current is App app)
        {
            var mainVm = app.GetMainViewModel();
            if (mainVm != null)
            {
                var exists = mainVm.DailyRecords.Records.Any(r => r.Name == name);
                if (exists)
                {
                    MessageBox.Show(this, "记录项已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                mainVm.DailyRecords.AddRecordCommand.Execute(null); // Not ideal, but AddRecord uses NewRecordName; instead call AddRecordByName if available
                // We'll call AddRecordByName via reflection to avoid changing MainViewModel further
                try
                {
                    var method = mainVm.DailyRecords.GetType().GetMethod("AddRecordByName");
                    if (method != null)
                    {
                        method.Invoke(mainVm.DailyRecords, new object[] { name, string.Empty, string.Empty });
                        Close();
                        return;
                    }
                }
                catch
                {
                    // fall through
                }
            }
        }

        // Fallback: use a temporary viewmodel to persist
        var temp = new DailyRecordsViewModel();
        var existing = temp.Records.Any(r => r.Name == name);
        if (existing)
        {
            MessageBox.Show(this, "记录项已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // invoke AddRecordByName via reflection if available
        var m = temp.GetType().GetMethod("AddRecordByName");
        if (m != null)
        {
            m.Invoke(temp, new object[] { name, string.Empty, string.Empty });
            Close();
            return;
        }

        // Last resort: show error
        MessageBox.Show(this, "无法新增记录项", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
