using System.Windows;
using System.Windows.Controls;

namespace FloatTodo.App.Views;

/// <summary>
/// 完整主面板的后台代码。
/// 这里仅处理少量与控件直接相关的事件，主要业务仍由 MainViewModel 承担。
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

    /// <summary>
    /// 主面板中的退出入口。
    /// 点击后退出整个 FloatTodo 程序，而不是只关闭或隐藏完整主面板。
    /// </summary>
    private void ExitApplicationButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
