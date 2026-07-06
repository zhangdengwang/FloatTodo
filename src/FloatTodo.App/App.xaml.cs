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
        _miniWidgetWindow.ToggleMainPanelRequested += (_, _) => ToggleMainPanel();
        _miniWidgetWindow.ExitRequested += (_, _) => Shutdown();
        MainWindow = _miniWidgetWindow;
        _miniWidgetWindow.Show();
    }

    private void ToggleMainPanel()
    {
        if (_mainPanelWindow == null)
        {
            _mainPanelWindow = new MainWindow();
            _mainPanelWindow.Closed += (_, _) => _mainPanelWindow = null;
        }

        if (_mainPanelWindow.IsVisible)
        {
            _mainPanelWindow.Hide();
        }
        else
        {
            _mainPanelWindow.Show();
            _mainPanelWindow.Activate();
        }
    }
}

