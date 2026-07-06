using System.Windows;

namespace FloatTodo.App;

/// <summary>
/// Application entry point.
/// </summary>
public partial class App : Application
{
    private MiniWidgetWindow? _miniWidgetWindow;
    private MainWindow? _mainPanelWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _miniWidgetWindow = new MiniWidgetWindow();
        MainWindow = _miniWidgetWindow;
        _miniWidgetWindow.Show();
    }
}

