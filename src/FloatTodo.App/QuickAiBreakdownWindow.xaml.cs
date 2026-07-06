using System;
using System.Linq;
using System.Windows;
using FloatTodo.App.Services;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

public partial class QuickAiBreakdownWindow : Window
{
    private readonly AiPlannerViewModel _vm;

    public QuickAiBreakdownWindow()
    {
        InitializeComponent();

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

        // Update status text visibility when planning
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
