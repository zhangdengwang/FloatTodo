using System.Windows;
using System.Windows.Input;

namespace FloatTodo.App;

/// <summary>
/// Main application window for the floating shell.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }
}