using System;
using System.Windows;
using System.Windows.Input;

namespace FloatTodo.App;

public partial class MiniWidgetWindow : Window
{
    private bool _isDragging;
    private bool _isLeftMouseDown;
    private Point _mouseDownPosition;

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
