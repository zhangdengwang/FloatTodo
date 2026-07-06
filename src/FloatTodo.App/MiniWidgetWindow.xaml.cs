using System;
using System.Windows;
using System.Windows.Input;

namespace FloatTodo.App;

public partial class MiniWidgetWindow : Window
{
    private Point _dragStart;
    private bool _isDragging;
    private MainWindow? _mainWindow;

    public MiniWidgetWindow()
    {
        InitializeComponent();
    }

    private void WidgetImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        FallbackText.Visibility = Visibility.Visible;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (!_isDragging)
            {
                _isDragging = true;
                _dragStart = e.GetPosition(this);
            }

            var screenPoint = PointToScreen(e.GetPosition(this));
            Left = screenPoint.X - _dragStart.X;
            Top = screenPoint.Y - _dragStart.Y;
        }
        else
        {
            _isDragging = false;
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
        {
            ToggleMainPanel();
        }

        _isDragging = false;
    }

    private void ToggleMainPanel()
    {
        if (_mainWindow is null)
        {
            _mainWindow = new MainWindow();
            _mainWindow.Closed += (_, _) => _mainWindow = null;
            _mainWindow.Show();
            return;
        }

        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
        }
    }

    private void ToggleMainWindowMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ToggleMainPanel();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
