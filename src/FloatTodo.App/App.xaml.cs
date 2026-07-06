using System.Windows;

namespace FloatTodo.App;

/// <summary>
/// Application entry point.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = new MainWindow();
        window.Show();
    }
}

