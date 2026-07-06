using System.Windows;

namespace FloatTodo.App;

/// <summary>
/// Application entry point.
/// </summary>
public partial class App : Application
{
    private MiniWidgetWindow? _miniWidgetWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnMainWindowClose;

        _miniWidgetWindow = new MiniWidgetWindow();
        MainWindow = _miniWidgetWindow;
        _miniWidgetWindow.Show();
    }
}

