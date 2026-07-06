using System.Windows;
using System.Windows.Controls;

namespace FloatTodo.App.Views;

/// <summary>
/// Code-behind for the main shell user control.
/// </summary>
public partial class ShellView : UserControl
{
    public ShellView()
    {
        InitializeComponent();
    }

    private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FloatTodo.App.ViewModels.MainViewModel viewModel)
            return;

        if (sender is PasswordBox pb)
        {
            viewModel.AiSettings.ApiKey = pb.Password;
        }
    }
}
