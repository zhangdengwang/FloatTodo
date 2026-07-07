using System.Windows;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

/// <summary>
/// WPF 程序入口。
/// 这里不直接打开完整主面板，而是先启动小桌宠窗口，让应用符合“桌面悬浮入口优先”的产品形态。
/// </summary>
public partial class App : Application
{
    // 小桌宠窗口是应用启动后默认可见的入口，生命周期基本贯穿整个程序运行过程。
    private MiniWidgetWindow? _miniWidgetWindow;

    // 完整主面板比较重，也不是主要入口，所以采用延迟创建：用户真正需要时才创建。
    private MainWindow? _mainPanelWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 使用显式退出模式后，隐藏/关闭普通窗口不会让应用误退出。
        // 真正退出统一走桌宠右键菜单中的“退出”，便于保持桌宠常驻。
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _miniWidgetWindow = new MiniWidgetWindow();

        // 完整主面板当前作为备用展示和调试界面保留，普通用户主要通过桌宠右键菜单和快捷窗口操作。
        // 桌宠不直接持有主面板创建细节；如果隐藏入口触发事件，仍由 App 统一创建/隐藏主面板。
        _miniWidgetWindow.ToggleMainPanelRequested += (_, _) => ToggleMainPanel();
        _miniWidgetWindow.ExitRequested += (_, _) => Shutdown();

        // MainWindow 指向桌宠窗口，保证 WPF 对当前主窗口有明确引用。
        MainWindow = _miniWidgetWindow;
        _miniWidgetWindow.Show();
    }

    /// <summary>
    /// 打开或隐藏备用完整主面板。
    /// 主面板不再作为普通用户入口，但保留为调试面板、备用展示界面和答辩兜底界面。
    /// 它只在第一次使用时创建，关闭后释放引用，下一次再按需重建。
    /// </summary>
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

    /// <summary>
    /// 供桌宠、快捷窗口等入口复用当前主面板中的 ViewModel。
    /// 如果主面板尚未创建，调用方会回退到存储服务或临时 ViewModel，避免强行打开主页面。
    /// </summary>
    public MainViewModel? GetMainViewModel()
    {
        return _mainPanelWindow?.DataContext as MainViewModel;
    }
}

