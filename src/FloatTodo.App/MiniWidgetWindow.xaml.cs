using System;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FloatTodo.App.ViewModels;
using FloatTodo.App.Services;
using FloatTodo.App.Models;

namespace FloatTodo.App;

public partial class MiniWidgetWindow : Window
{
    public event EventHandler? ToggleMainPanelRequested;
    public event EventHandler? ExitRequested;
    private QuickDailyRecordsWindow? _quickDailyRecordsWindow;
    private QuickProjectListWindow? _quickProjectListWindow;
    private readonly DispatcherTimer _reminderTimer;

    public MiniWidgetWindow()
    {
        InitializeComponent();
        _reminderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _reminderTimer.Tick += (_, _) =>
        {
            RefreshDailyReminderState();
            RefreshPetState();
        };
        Loaded += (_, _) =>
        {
            _ = new DailyRecordsViewModel();
            RefreshPetState();
            RefreshDailyReminderState();
            _reminderTimer.Start();
        };
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
        try
        {
            OpenTaskList(QuickTaskFilter.Unfinished);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"打开任务列表失败：{ex.Message}", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenDueSoonTasks_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OpenTaskList(QuickTaskFilter.DueSoon);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"打开任务列表失败：{ex.Message}", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenTaskList(QuickTaskFilter filter)
    {
        var tasks = new TaskStorageService().Load();
        var now = DateTime.Now;
        var dueSoonLimit = now.AddHours(24);
        var displayItems = new List<QuickTaskListItem>();

        foreach (var task in tasks)
        {
            if (task.IsProject || task.Status == FloatTodo.App.Models.TaskStatus.Done)
            {
                continue;
            }

            if (filter == QuickTaskFilter.DueSoon)
            {
                if (!IsUrgentTask(task, dueSoonLimit))
                {
                    continue;
                }
            }

            var dueTimeDisplay = task.DueTime.HasValue
                ? task.DueTime.Value.ToString("yyyy-MM-dd HH:mm")
                : "无截止时间";
            var title = task.Title;

            if (filter == QuickTaskFilter.DueSoon && task.DueTime.HasValue)
            {
                title += task.DueTime.Value < now ? "  (已逾期)" : "  (即将截止)";
            }

            displayItems.Add(new QuickTaskListItem(
                title,
                task.Priority.ToString(),
                dueTimeDisplay));
        }

        var titleText = filter == QuickTaskFilter.Unfinished ? "未完成任务" : "快截止任务";
        var emptyText = filter == QuickTaskFilter.Unfinished
            ? "当前没有未完成任务。"
            : "当前没有快截止任务。";
        var window = new QuickTaskListWindow(titleText, emptyText, displayItems)
        {
            Owner = this
        };
        window.Show();
    }

    private void OpenAddProject_Click(object sender, RoutedEventArgs e)
    {
        var window = new QuickAddProjectWindow
        {
            Owner = this
        };
        window.ShowDialog();
        RefreshPetState();
        _quickProjectListWindow?.RefreshProjects();
    }

    private void OpenProjectList_Click(object sender, RoutedEventArgs e)
    {
        if (_quickProjectListWindow is { IsVisible: true })
        {
            _quickProjectListWindow.RefreshProjects();
            _quickProjectListWindow.Activate();
            return;
        }

        _quickProjectListWindow = new QuickProjectListWindow
        {
            Owner = this
        };
        _quickProjectListWindow.Closed += (_, _) => _quickProjectListWindow = null;
        _quickProjectListWindow.Show();
    }

    private void OpenAddDailyRecord_Click(object sender, RoutedEventArgs e)
    {
        var w = new QuickAddDailyRecordWindow();
        w.Owner = this;
        w.ShowDialog();
        RefreshDailyRecordMenuHeaders();
        RefreshDailyReminderState();
        _quickDailyRecordsWindow?.RefreshRecords();
    }

    private void OpenDailyRecords_Click(object sender, RoutedEventArgs e)
    {
        if (_quickDailyRecordsWindow is { IsVisible: true })
        {
            _quickDailyRecordsWindow.RefreshRecords();
            _quickDailyRecordsWindow.Activate();
            return;
        }

        _quickDailyRecordsWindow = new QuickDailyRecordsWindow
        {
            Owner = this
        };
        _quickDailyRecordsWindow.Closed += (_, _) => _quickDailyRecordsWindow = null;
        _quickDailyRecordsWindow.Show();
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
        try
        {
            DailyRecordsViewModel recordsViewModel;
            if (Application.Current is App app && app.GetMainViewModel() is { } mainViewModel)
            {
                recordsViewModel = mainViewModel.DailyRecords;
            }
            else
            {
                recordsViewModel = new DailyRecordsViewModel();
            }

            if (!recordsViewModel.IncrementRecordByName(name, description, iconText))
            {
                MessageBox.Show(this, "日常记录保存失败，请稍后重试。", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RefreshDailyRecordMenuHeaders();
            RefreshDailyReminderState();
            _quickDailyRecordsWindow?.RefreshRecords();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"日常记录保存失败：{ex.Message}", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        RefreshDailyRecordMenuHeaders();
    }

    private void RefreshDailyRecordMenuHeaders()
    {
        try
        {
            var records = GetDailyRecordsSnapshot();

            DrinkWaterMenuItem.Header = BuildDailyRecordMenuHeader("喝水", records);
            RestEyesMenuItem.Header = BuildDailyRecordMenuHeader("休息眼睛", records);
            StandUpMenuItem.Header = BuildDailyRecordMenuHeader("起身活动", records);
        }
        catch
        {
            DrinkWaterMenuItem.Header = BuildDailyRecordMenuHeader("喝水", []);
            RestEyesMenuItem.Header = BuildDailyRecordMenuHeader("休息眼睛", []);
            StandUpMenuItem.Header = BuildDailyRecordMenuHeader("起身活动", []);
        }
    }

    private static string BuildDailyRecordMenuHeader(string name, IEnumerable<DailyRecordItem> records)
    {
        var record = records.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.Ordinal));
        var count = record?.LastRecordTime?.Date == DateTime.Today ? record.TodayCount : 0;
        var lastRecordTime = record?.LastRecordTime?.ToString("HH:mm") ?? "未记录";
        var threshold = record?.ReminderThresholdMinutes
            ?? DailyRecordsViewModel.GetDefaultReminderThresholdMinutes(name);
        var thresholdText = threshold is > 0 ? $"{threshold}分钟" : "未启用";
        return $"{name} +1（次数：{count}，上次：{lastRecordTime}，阈值：{thresholdText}）";
    }

    private static bool IsUrgentTask(TaskItem task, DateTime dueSoonLimit)
    {
        return !task.IsProject &&
               task.Status != FloatTodo.App.Models.TaskStatus.Done &&
               task.DueTime.HasValue &&
               task.DueTime.Value <= dueSoonLimit;
    }

    private static List<DailyRecordItem> GetDailyRecordsSnapshot()
    {
        return Application.Current is App app && app.GetMainViewModel() is { } mainViewModel
            ? mainViewModel.DailyRecords.Records.ToList()
            : new DailyRecordStorageService().Load();
    }

    private void RefreshDailyReminderState()
    {
        try
        {
            var records = GetDailyRecordsSnapshot();
            var now = DateTime.Now;
            DailyRecordItem? earliestReminder = null;
            var earliestTriggerTime = DateTime.MaxValue;

            foreach (var record in records)
            {
                if (record.ReminderThresholdMinutes is not > 0)
                {
                    continue;
                }

                var triggerTime = record.LastRecordTime.HasValue
                    ? record.LastRecordTime.Value.AddMinutes(record.ReminderThresholdMinutes.Value)
                    : DateTime.MinValue;

                if (now < triggerTime || triggerTime >= earliestTriggerTime)
                {
                    continue;
                }

                earliestReminder = record;
                earliestTriggerTime = triggerTime;
            }

            if (earliestReminder == null)
            {
                ReminderRoot.Visibility = Visibility.Collapsed;
                ReminderText.Text = string.Empty;
                return;
            }

            ReminderText.Text = $"请{earliestReminder.Name}";
            ReminderRoot.Visibility = Visibility.Visible;
        }
        catch
        {
            ReminderRoot.Visibility = Visibility.Collapsed;
        }
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
        _quickProjectListWindow?.RefreshProjects();
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
        _reminderTimer.Stop();
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

            var unfinished = tasks.Count(t =>
                !t.IsProject &&
                t.Status != FloatTodo.App.Models.TaskStatus.Done);
            var now = DateTime.Now;
            var dueSoonLimit = now.AddHours(24);
            var dueSoon = tasks.Count(t => IsUrgentTask(t, dueSoonLimit));

            if (unfinished == 0)
            {
                PetImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Pet/pet_no_task.png"));
                BadgeRoot.Visibility = Visibility.Collapsed;
                BadgeText.Text = string.Empty;
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
                BadgeRoot.Visibility = Visibility.Collapsed;
                BadgeText.Text = string.Empty;
            }
        }
        catch
        {
            // keep pet stable
        }
    }
}
