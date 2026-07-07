using System;
using System.Linq;
using System.Windows;
using FloatTodo.App.Services;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

/// <summary>
/// 右键菜单打开的 AI 项目拆解窗口。
/// 窗口层只负责收集输入和触发命令，真正的 AI 调用与任务保存逻辑放在 AiPlannerViewModel 中。
/// </summary>
public partial class QuickAiBreakdownWindow : Window
{
    private readonly AiPlannerViewModel _vm;

    public QuickAiBreakdownWindow()
    {
        InitializeComponent();

        // 如果完整主面板已打开，复用它的 MainViewModel，加入的项目和小任务会立刻反映到主面板。
        // 如果主面板未打开，则创建临时 MainViewModel，仍然通过同一套 JSON 存储保存。
        MainViewModel? mainVm = null;
        if (Application.Current is App app)
        {
            mainVm = app.GetMainViewModel();
        }

        if (mainVm == null)
            mainVm = new MainViewModel();

        var settingsSvc = new ApiSettingsService();
        var service = new AiPlannerService(settingsSvc);
        _vm = new AiPlannerViewModel(mainVm, service);
        DataContext = _vm;

        // AI 调用是异步过程，期间显示“正在拆解”并禁用生成按钮，避免用户重复提交。
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AiPlannerViewModel.IsPlanning))
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Visibility = _vm.IsPlanning ? Visibility.Visible : Visibility.Collapsed;
                    GenerateButton.IsEnabled = !_vm.IsPlanning;
                });
            }
        };
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        // 项目名称和描述先同步到 ViewModel，再由命令调用 AI 服务。
        _vm.ProjectName = ProjectNameBox.Text?.Trim() ?? string.Empty;
        _vm.ProjectDescription = ProjectDescBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_vm.ProjectName))
        {
            MessageBox.Show(this, "请填写项目名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_vm.GenerateCommand.CanExecute(null))
        {
            _vm.GenerateCommand.Execute(null);
        }
    }

    private void AddSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        // 候选任务不会自动保存，必须由用户勾选并点击“加入选中任务”后才写入待办。
        _vm.ProjectName = ProjectNameBox.Text?.Trim() ?? string.Empty;

        if (_vm.AddSelectedToTasksCommand.CanExecute(null))
        {
            _vm.AddSelectedToTasksCommand.Execute(null);

            var added = _vm.Candidates.Count(c => c.IsAdded);
            if (added > 0)
            {
                Close();
            }
        }
    }
}
