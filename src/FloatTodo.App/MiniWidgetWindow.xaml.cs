using System;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Windows.Media.Imaging;
using FloatTodo.App.ViewModels;
using FloatTodo.App.Services;
using FloatTodo.App.Models;

namespace FloatTodo.App;

public partial class MiniWidgetWindow : Window
{
    public event EventHandler? ToggleMainPanelRequested;
    public event EventHandler? ExitRequested;

    public MiniWidgetWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshPetState();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
                ClampToScreen();
            }
            catch
            {
                // 忽略拖动异常，不关闭窗口
            }
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        // XAML 中保留绑定，但左键拖动由 DragMove() 处理。
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // 左键松开时不触发主面板切换。
    }

    private void ClampToScreen()
    {
        var workArea = SystemParameters.WorkArea;

        var newLeft = Left;
        var newTop = Top;

        if (newLeft < workArea.Left)
        {
            newLeft = workArea.Left;
        }

        if (newTop < workArea.Top)
        {
            newTop = workArea.Top;
        }

        if (newLeft + Width > workArea.Right)
        {
            newLeft = workArea.Right - Width;
        }

        if (newTop + Height > workArea.Bottom)
        {
            newTop = workArea.Bottom - Height;
        }

        Left = newLeft;
        Top = newTop;
    }

    private void ToggleMainWindowMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ToggleMainPanelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OpenUnfinishedTasks_Click(object sender, RoutedEventArgs e)
    {
        var w = new QuickTaskListWindow(QuickTaskFilter.Unfinished);
        w.Owner = this;
        w.ShowDialog();
    }

    private void OpenDueSoonTasks_Click(object sender, RoutedEventArgs e)
    {
        var w = new QuickTaskListWindow(QuickTaskFilter.DueSoon);
        w.Owner = this;
        w.ShowDialog();
    }

    private void OpenProjectProgress_Click(object sender, RoutedEventArgs e)
    {
        var w = new QuickProjectProgressWindow();
        w.Owner = this;
        w.ShowDialog();
    }

    private void OpenAddDailyRecord_Click(object sender, RoutedEventArgs e)
    {
        var w = new QuickAddDailyRecordWindow();
        w.Owner = this;
        w.ShowDialog();
    }

    private void OpenDailyRecords_Click(object sender, RoutedEventArgs e)
    {
        var w = new QuickDailyRecordsWindow();
        w.Owner = this;
        w.ShowDialog();
    }

    private void DrinkWaterMenuItem_Click(object sender, RoutedEventArgs e)
    {
        AddDailyRecord("喝水", "记录今日喝水次数", "💧");
    }

    private void RestEyesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        AddDailyRecord("休息眼睛", "记录今日休息眼睛次数", "👀");
    }

    private void StandUpMenuItem_Click(object sender, RoutedEventArgs e)
    {
        AddDailyRecord("起身活动", "记录今日起身活动次数", "🦶");
    }

    private void AddDailyRecord(string name, string description, string iconText)
    {
        if (Application.Current is not App app)
        {
            MessageBox.Show(this, $"{name} +1 已记录", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var mainViewModel = app.GetMainViewModel();
        if (mainViewModel != null)
        {
            mainViewModel.DailyRecords.IncrementRecordByName(name, description, iconText);
            MessageBox.Show(this, $"{name} +1 已记录", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var temp = new DailyRecordsViewModel();
        temp.IncrementRecordByName(name, description, iconText);
        MessageBox.Show(this, $"{name} +1 已记录", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenQuickAddTaskWindow_Click(object sender, RoutedEventArgs e)
    {
        var quickAdd = new QuickAddTaskWindow();
        quickAdd.Owner = this;
        quickAdd.ShowDialog();
        RefreshPetState();
    }

    private void OnPlaceholderMenuItemClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "功能入口已创建，后续接入小面板",
            "FloatTodo",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenAiBreakdown_Click(object sender, RoutedEventArgs e)
    {
        var w = new QuickAiBreakdownWindow();
        w.Owner = this;
        w.ShowDialog();
        RefreshPetState();
    }

    private void ViewAiCandidates_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this, "请先使用新建项目拆解生成候选任务。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void MiniWidgetWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Application.Current.Shutdown();
    }

    public void RefreshPetState()
    {
        try
        {
            MainViewModel? mainVm = null;
            if (Application.Current is App app)
            {
                mainVm = app.GetMainViewModel();
            }

            var tasks = Enumerable.Empty<TaskItem>();
            if (mainVm != null)
                tasks = mainVm.Tasks.ToList();
            else
            {
                var storage = new TaskStorageService();
                tasks = storage.Load();
            }

            var unfinished = tasks.Count(t => t.Status != FloatTodo.App.Models.TaskStatus.Done);
            var now = DateTime.Now;
            var dueSoon = tasks.Count(t => t.Status != FloatTodo.App.Models.TaskStatus.Done && t.DueTime.HasValue && t.DueTime.Value <= now.AddHours(24));

            if (unfinished == 0)
            {
                PetImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Pet/pet_no_task.png"));
                BadgeRoot.Visibility = Visibility.Collapsed;
            }
            else if (dueSoon > 0)
            {
                PetImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Pet/pet_urgent.png"));
                BadgeRoot.Visibility = Visibility.Visible;
                BadgeText.Text = dueSoon >= 100 ? "99+" : dueSoon.ToString();
            }
            else
            {
                PetImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Pet/pet_has_task.png"));
                BadgeRoot.Visibility = unfinished > 0 ? Visibility.Visible : Visibility.Collapsed;
                BadgeText.Text = unfinished >= 100 ? "99+" : unfinished.ToString();
            }
        }
        catch
        {
            // keep pet stable
        }
    }
}
