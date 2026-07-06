using System;
using System.Windows;
using System.Windows.Input;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

public partial class MiniWidgetWindow : Window
{
    public event EventHandler? ToggleMainPanelRequested;
    public event EventHandler? ExitRequested;

    public MiniWidgetWindow()
    {
        InitializeComponent();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
                ClampToScreen();
            }
            catch
            {
                // 忽略拖动异常，不关闭窗口
            }
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        // XAML 中保留绑定，但左键拖动由 DragMove() 处理。
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // 左键松开时不触发主面板切换。
    }

    private void ClampToScreen()
    {
        var workArea = SystemParameters.WorkArea;

        var newLeft = Left;
        var newTop = Top;

        if (newLeft < workArea.Left)
        {
            newLeft = workArea.Left;
        }

        if (newTop < workArea.Top)
        {
            newTop = workArea.Top;
        }

        if (newLeft + Width > workArea.Right)
        {
            newLeft = workArea.Right - Width;
        }

        if (newTop + Height > workArea.Bottom)
        {
            newTop = workArea.Bottom - Height;
        }

        Left = newLeft;
        Top = newTop;
    }

    private void ToggleMainWindowMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ToggleMainPanelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void DrinkWaterMenuItem_Click(object sender, RoutedEventArgs e)
    {
        AddDailyRecord("喝水", "记录今日喝水次数", "💧");
    }

    private void RestEyesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        AddDailyRecord("休息眼睛", "记录今日休息眼睛次数", "👀");
    }

    private void StandUpMenuItem_Click(object sender, RoutedEventArgs e)
    {
        AddDailyRecord("起身活动", "记录今日起身活动次数", "🦶");
    }

    private void AddDailyRecord(string name, string description, string iconText)
    {
        if (Application.Current is not App app)
        {
            MessageBox.Show(this, $"{name} +1 已记录", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var mainViewModel = app.GetMainViewModel();
        if (mainViewModel != null)
        {
            mainViewModel.DailyRecords.IncrementRecordByName(name, description, iconText);
            MessageBox.Show(this, $"{name} +1 已记录", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var temp = new DailyRecordsViewModel();
        temp.IncrementRecordByName(name, description, iconText);
        MessageBox.Show(this, $"{name} +1 已记录", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenQuickAddTaskWindow_Click(object sender, RoutedEventArgs e)
    {
        var quickAdd = new QuickAddTaskWindow();
        quickAdd.Owner = this;
        quickAdd.ShowDialog();
    }

    private void OnPlaceholderMenuItemClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "功能入口已创建，后续接入小面板",
            "FloatTodo",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void MiniWidgetWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
