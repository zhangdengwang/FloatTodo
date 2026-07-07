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

/// <summary>
/// 小桌宠悬浮窗口。
/// 负责显示桌宠图片、任务红点、日常提醒、截止任务提醒，并作为右键菜单入口。
/// </summary>
public partial class MiniWidgetWindow : Window
{
    // 完整主面板作为备用展示和调试界面保留；普通用户主要通过桌宠右键菜单和快捷窗口操作。
    // 如果后续需要临时打开完整面板，仍可复用该事件把请求交给 App 处理。
    public event EventHandler? ToggleMainPanelRequested;
    public event EventHandler? ExitRequested;

    // 快捷查看窗口使用字段保存引用，避免用户重复点击菜单时打开多个相同窗口。
    private QuickDailyRecordsWindow? _quickDailyRecordsWindow;
    private QuickProjectListWindow? _quickProjectListWindow;

    // 定时刷新日常提醒和任务红点。
    // 这样即使用户没有打开菜单，任务进入 24 小时范围时桌宠状态也能自动更新。
    private readonly DispatcherTimer _reminderTimer;

    // 当前正在提醒文字区域显示的日常记录项名称。
    // 双击提醒气泡时只给这一项 +1，避免误操作其他日常记录。
    private string? _currentReminderRecordName;

    // 当前正在显示的截止任务提醒 Id。
    // 双击截止提醒时只打开这一条任务详情，避免误触桌宠图片或其他透明区域。
    private Guid? _currentDueReminderTaskId;

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
            // 初始化日常记录默认项，保证喝水/休息眼睛/起身活动这些快捷入口首次运行即可使用。
            _ = new DailyRecordsViewModel();
            RefreshPetState();
            RefreshDailyReminderState();
            _reminderTimer.Start();
        };
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        // 左键按下时只负责拖动窗口。
        // 产品要求左键单击不打开主面板，主面板只通过右键菜单显式打开。
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
        // 拖动结束后把窗口限制在工作区内，防止桌宠被拖到屏幕外找不回来。
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
        // 备用完整面板入口：当前普通右键菜单不再显示该入口。
        // 保留方法是为了调试、答辩兜底或后续隐藏入口复用。
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
        // 任务列表由窗口按需重新加载，完成/删除后会刷新列表和桌宠红点。
        // 加载过程仍然是一次性读取现有 JSON，不使用阻塞等待或循环轮询。
        IReadOnlyCollection<QuickTaskListItem> LoadDisplayItems()
        {
            return BuildTaskListItems(filter);
        }

        var titleText = filter == QuickTaskFilter.Unfinished ? "未完成任务" : "快截止任务";
        var emptyText = filter == QuickTaskFilter.Unfinished
            ? "当前没有未完成任务。"
            : "当前没有快截止任务。";
        var window = new QuickTaskListWindow(titleText, emptyText, LoadDisplayItems, RefreshPetState)
        {
            Owner = this
        };
        window.Show();
    }

    private static IReadOnlyCollection<QuickTaskListItem> BuildTaskListItems(QuickTaskFilter filter)
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
                // 快截止列表和桌宠红点使用同一规则：24 小时内截止或已经逾期的未完成非项目任务。
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
                task.Id,
                title,
                task.Priority.ToString(),
                task.DueTime,
                dueTimeDisplay,
                task.ProjectName,
                task.Status == FloatTodo.App.Models.TaskStatus.Done ? "已完成" : "未完成",
                task.Description));
        }

        return displayItems
            .OrderBy(item => GetDueSortGroup(item.DueTime, now, dueSoonLimit))
            .ThenBy(item => item.DueTime ?? DateTime.MaxValue)
            .ThenBy(item => item.Title, StringComparer.CurrentCulture)
            .ToList();
    }

    private static int GetDueSortGroup(DateTime? dueTime, DateTime now, DateTime dueSoonLimit)
    {
        if (!dueTime.HasValue)
        {
            return 3;
        }

        if (dueTime.Value < now)
        {
            return 0;
        }

        return dueTime.Value <= dueSoonLimit ? 1 : 2;
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
        _quickDailyRecordsWindow.Closed += (_, _) =>
        {
            _quickDailyRecordsWindow = null;
            RefreshDailyRecordMenuHeaders();
            RefreshDailyReminderState();
        };
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
            // 如果完整主面板已打开，复用它的 DailyRecordsViewModel，保证界面状态和存储状态同步。
            // 如果主面板未打开，则创建临时 ViewModel，通过同一个 DailyRecordStorageService 保存。
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

    /// <summary>
    /// 日常提醒文字区域的双击快捷操作。
    /// 这是为了减少“右键菜单 → 日常记录 → +1”的操作层级，只处理当前正在显示的提醒项。
    /// </summary>
    private void DailyReminder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 提醒气泡区域不参与窗口拖动，避免双击时同时触发 DragMove。
        e.Handled = true;

        if (e.ClickCount == 2)
        {
            CompleteCurrentDailyReminder();
        }
    }

    /// <summary>
    /// 给当前显示的日常提醒项记录一次。
    /// 保存仍复用现有日常记录 +1 逻辑，不新增存储路径或 JSON 格式。
    /// </summary>
    private void CompleteCurrentDailyReminder()
    {
        if (string.IsNullOrWhiteSpace(_currentReminderRecordName))
        {
            return;
        }

        AddDailyRecord(_currentReminderRecordName, string.Empty, string.Empty);
    }

    /// <summary>
    /// 截止任务提醒区域的双击操作。
    /// 双击只打开当前提醒任务的详情窗口，不直接完成任务，避免用户误操作导致任务状态变化。
    /// </summary>
    private void DueReminder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 截止提醒气泡独立处理鼠标事件，避免双击时触发窗口拖动。
        e.Handled = true;

        if (e.ClickCount == 2)
        {
            OpenCurrentDueReminderTaskDetail();
        }
    }

    /// <summary>
    /// 打开当前截止提醒对应的任务详情。
    /// 详情窗口中的完成/删除操作仍复用现有任务存储服务，并在操作后刷新桌宠状态。
    /// </summary>
    private void OpenCurrentDueReminderTaskDetail()
    {
        if (!_currentDueReminderTaskId.HasValue)
        {
            return;
        }

        try
        {
            var task = GetTaskSnapshot()
                .FirstOrDefault(item => item.Id == _currentDueReminderTaskId.Value && !item.IsProject);
            if (task == null)
            {
                RefreshDueTaskReminderState();
                return;
            }

            var detail = new QuickTaskDetailWindow(
                new TaskDetailDisplayItem(
                    task.Id,
                    task.Title,
                    task.Priority.ToString(),
                    task.DueTime.HasValue ? task.DueTime.Value.ToString("yyyy-MM-dd HH:mm") : "无截止时间",
                    task.ProjectName,
                    task.Status == FloatTodo.App.Models.TaskStatus.Done ? "已完成" : "未完成",
                    task.Description),
                () =>
                {
                    RefreshPetState();
                    _quickProjectListWindow?.RefreshProjects();
                })
            {
                Owner = this
            };
            detail.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"打开任务详情失败：{ex.Message}", "FloatTodo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        // 右键菜单每次打开时刷新次数和上次记录时间，避免菜单文字停留在旧状态。
        RefreshDailyRecordMenuHeaders();
    }

    /// <summary>
    /// 刷新日常记录二级菜单的显示文字。
    /// 菜单项展示当天次数、上次记录时间和提醒阈值，让用户不用打开窗口也能看到状态。
    /// </summary>
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
        // 次数按“今天”统计：如果最后记录不是今天，菜单中显示 0，符合日常打卡的直觉。
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
        // 项目父节点只用于组织小任务，不应计入桌宠红点。
        // 红点只统计 24 小时内截止或已经逾期的普通任务 / 项目小任务。
        return !task.IsProject &&
               task.Status != FloatTodo.App.Models.TaskStatus.Done &&
               task.DueTime.HasValue &&
               task.DueTime.Value <= dueSoonLimit;
    }

    private static List<DailyRecordItem> GetDailyRecordsSnapshot()
    {
        // 优先从已打开的主面板 ViewModel 取数据；没有主面板时从 JSON 读取快照。
        // 这样既能保持界面同步，也不会因为没打开主面板而无法使用桌宠菜单。
        return Application.Current is App app && app.GetMainViewModel() is { } mainViewModel
            ? mainViewModel.DailyRecords.Records.ToList()
            : new DailyRecordStorageService().Load();
    }

    private void RefreshDailyReminderState()
    {
        try
        {
            // 日常提醒不是弹窗，而是桌宠旁边的一小段状态文字。
            // 选择最早达到提醒阈值的记录项显示，避免同时出现多条提示挤占桌宠区域。
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
                _currentReminderRecordName = null;
                ReminderRoot.Visibility = Visibility.Collapsed;
                ReminderText.Text = string.Empty;
                return;
            }

            _currentReminderRecordName = earliestReminder.Name;
            ReminderText.Text = $"请{earliestReminder.Name}";
            ReminderRoot.Visibility = Visibility.Visible;
        }
        catch
        {
            _currentReminderRecordName = null;
            ReminderRoot.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// 刷新截止任务提醒文字。
    /// 规则与桌宠红点保持一致：只提醒 24 小时内截止或已经逾期的未完成非项目任务，并只展示最紧急的一条。
    /// </summary>
    private void RefreshDueTaskReminderState()
    {
        try
        {
            var now = DateTime.Now;
            var dueSoonLimit = now.AddHours(24);
            var task = GetTaskSnapshot()
                .Where(item => IsUrgentTask(item, dueSoonLimit))
                .OrderBy(item => GetDueSortGroup(item.DueTime, now, dueSoonLimit))
                .ThenBy(item => item.DueTime ?? DateTime.MaxValue)
                .ThenBy(item => item.Title, StringComparer.CurrentCulture)
                .FirstOrDefault();

            if (task == null)
            {
                _currentDueReminderTaskId = null;
                DueReminderText.Text = string.Empty;
                DueReminderRoot.Visibility = Visibility.Collapsed;
                DueReminderRoot.ToolTip = "双击查看任务详情";
                return;
            }

            _currentDueReminderTaskId = task.Id;
            DueReminderText.Text = task.Title;
            DueReminderRoot.ToolTip = task.DueTime.HasValue
                ? $"双击查看任务详情\n截止时间：{task.DueTime.Value:yyyy-MM-dd HH:mm}"
                : "双击查看任务详情";
            DueReminderRoot.Visibility = Visibility.Visible;
        }
        catch
        {
            _currentDueReminderTaskId = null;
            DueReminderText.Text = string.Empty;
            DueReminderRoot.Visibility = Visibility.Collapsed;
        }
    }

    private void OpenQuickAddTaskWindow_Click(object sender, RoutedEventArgs e)
    {
        // 快速新增任务完成后立即刷新桌宠，保证红点和三状态图片及时变化。
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

    /// <summary>
    /// 显示项目关于信息。
    /// 关于入口只展示静态说明，不访问网络和本地存储，因此不会阻塞桌宠菜单。
    /// </summary>
    private void ShowAbout_Click(object sender, RoutedEventArgs e)
    {
        const string aboutText =
            "FloatTodo\n\n" +
            "AI 项目拆解与桌宠化悬浮待办工具\n\n" +
            "主要功能：\n" +
            "- 桌宠悬浮入口\n" +
            "- 右键多级菜单\n" +
            "- 普通任务与项目任务管理\n" +
            "- AI 项目拆解\n" +
            "- 日常记录与阈值提醒\n" +
            "- 24 小时内截止任务红点提醒\n" +
            "- 本地 JSON 数据保存\n" +
            "- Windows 自包含发布\n\n" +
            "操作说明：\n" +
            "- 左键按住桌宠可拖动\n" +
            "- 右键桌宠打开功能菜单\n" +
            "- AI 拆解需要配置 DeepSeek API Key 和网络\n" +
            "- 普通任务、项目、日常记录不需要 API Key";

        MessageBox.Show(this, aboutText, "关于 FloatTodo", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 打开轻量 API 设置窗口。
    /// API Key 设置属于右键菜单的常用配置入口，不需要先打开完整主面板。
    /// </summary>
    private void OpenApiSettings_Click(object sender, RoutedEventArgs e)
    {
        var window = new QuickApiSettingsWindow
        {
            Owner = this
        };
        window.ShowDialog();
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
        // 退出统一交给 App 处理，配合 OnExplicitShutdown 保证应用真正结束。
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void MiniWidgetWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // 关闭桌宠意味着用户明确退出应用，停止定时器后关闭整个 WPF 程序。
        _reminderTimer.Stop();
        Application.Current.Shutdown();
    }

    /// <summary>
    /// 刷新桌宠任务状态。
    /// 根据未完成任务和 24 小时内截止/逾期任务数量，切换三状态图片并决定是否显示红点。
    /// </summary>
    public void RefreshPetState()
    {
        try
        {
            // 如果完整主面板已打开，优先使用内存中的任务集合；否则直接从 JSON 读取。
            // 这样桌宠不依赖主面板，也能在默认悬浮入口模式下正常显示任务状态。
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

            // 三状态图片中的“有无任务”看所有未完成非项目任务。
            var unfinished = tasks.Count(t =>
                !t.IsProject &&
                t.Status != FloatTodo.App.Models.TaskStatus.Done);
            var now = DateTime.Now;
            var dueSoonLimit = now.AddHours(24);

            // 红点数字只看紧急任务，不再显示普通未完成任务数量。
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

            // 截止任务提醒与红点使用同一组筛选规则，任务新增/完成/删除后跟随桌宠状态一起刷新。
            RefreshDueTaskReminderState();
        }
        catch
        {
            // keep pet stable
        }
    }

    private static IReadOnlyCollection<TaskItem> GetTaskSnapshot()
    {
        // 优先读取主面板 ViewModel 中的内存任务；主面板未创建时，从同一个 tasks.json 读取。
        // 这样截止提醒、红点和快捷任务列表始终使用同一套任务数据。
        if (Application.Current is App app && app.GetMainViewModel() is { } mainVm)
        {
            return mainVm.Tasks.ToList();
        }

        return new TaskStorageService().Load();
    }
}
