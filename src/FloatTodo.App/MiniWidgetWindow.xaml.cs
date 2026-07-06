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

        _isLeftMouseDown = true;
        _mouseDownPosition = e.GetPosition(this);
        CaptureMouse();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isLeftMouseDown || _isDragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        var deltaX = Math.Abs(currentPosition.X - _mouseDownPosition.X);
        var deltaY = Math.Abs(currentPosition.Y - _mouseDownPosition.Y);

        if (deltaX > 5 || deltaY > 5)
        {
            try
            {
                _isDragging = true;
                DragMove();
                ClampToScreen();
            }
            catch
            {
                // 忽略拖动异常，不关闭窗口
            }
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isLeftMouseDown && !_isDragging)
        {
            ToggleMainPanelRequested?.Invoke(this, EventArgs.Empty);
        }

        _isLeftMouseDown = false;
        _isDragging = false;
        ReleaseMouseCapture();
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

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void MiniWidgetWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
