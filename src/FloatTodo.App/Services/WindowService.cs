using System.Windows;

namespace FloatTodo.App.Services;

/// <summary>
/// Provides lightweight helper behavior for the floating window shell.
/// </summary>
public static class WindowService
{
    public static void ConfigureFloatingWindow(Window window)
    {
        window.Width = 360;
        window.Height = 520;
        window.Topmost = true;
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.Title = "FloatTodo";

        // Keep the shell visually simple for the first iteration.
        window.Background = System.Windows.Media.Brushes.Transparent;
        window.AllowsTransparency = true;
    }
}
