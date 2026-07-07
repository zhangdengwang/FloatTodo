using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FloatTodo.App.Models;
using FloatTodo.App.Services;

namespace FloatTodo.App.ViewModels;

/// <summary>
/// 日常记录 ViewModel。
/// 负责维护记录列表、处理快捷 +1、补齐默认记录，并通过 DailyRecordStorageService 保存到 JSON。
/// </summary>
public sealed class DailyRecordsViewModel : INotifyPropertyChanged
{
    private readonly DailyRecordStorageService _storage;
    private string _newRecordName = string.Empty;
    private string _newRecordDescription = string.Empty;
    private string _newRecordIcon = string.Empty;

    public DailyRecordsViewModel()
    {
        _storage = new DailyRecordStorageService();
        Records = new ObservableCollection<DailyRecordItem>();

        AddRecordCommand = new RelayCommand(_ => AddRecord(), _ => !string.IsNullOrWhiteSpace(NewRecordName));
        IncrementCommand = new RelayCommand(param => IncrementRecord((DailyRecordItem)param!), _ => true);

        // 加载旧记录时，如果最后记录时间不是今天，则当天次数归零。
        // 这样“今日次数”不会跨天累计。
        var needsSave = false;
        var items = _storage.Load();
        foreach (var record in items)
        {
            if (record.LastRecordTime is null || record.LastRecordTime.Value.Date != DateTime.Today)
            {
                record.TodayCount = 0;
            }

            Records.Add(record);
        }

        needsSave |= EnsureDefaultRecord("喝水", "记录今日喝水次数", "💧", 60);
        needsSave |= EnsureDefaultRecord("休息眼睛", "记录今日休息眼睛次数", "👀", 45);
        needsSave |= EnsureDefaultRecord("起身活动", "记录今日起身活动次数", "🦶", 90);

        // 默认记录或旧数据阈值被补齐后，立即写回同一个 daily-records.json。
        if (needsSave)
        {
            Save();
        }
    }

    public ObservableCollection<DailyRecordItem> Records { get; }

    public string NewRecordName
    {
        get => _newRecordName;
        set { if (_newRecordName != value) { _newRecordName = value; OnPropertyChanged(); ((RelayCommand)AddRecordCommand).RaiseCanExecuteChanged(); } }
    }

    public string NewRecordDescription
    {
        get => _newRecordDescription;
        set { if (_newRecordDescription != value) { _newRecordDescription = value; OnPropertyChanged(); } }
    }

    public string NewRecordIcon
    {
        get => _newRecordIcon;
        set { if (_newRecordIcon != value) { _newRecordIcon = value; OnPropertyChanged(); } }
    }

    public ICommand AddRecordCommand { get; }
    public ICommand IncrementCommand { get; }

    public bool AddRecordByName(
        string name,
        string description,
        string iconText,
        int? reminderThresholdMinutes = null)
    {
        // 新增日常记录只创建记录项，不自动 +1。
        // 这样“新增记录”和“记录一次”两个动作语义清晰。
        var trimmedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return false;
        }

        if (Records.Any(r => string.Equals(r.Name, trimmedName, StringComparison.Ordinal)))
        {
            return false;
        }

        Records.Add(new DailyRecordItem
        {
            Name = trimmedName,
            Description = description?.Trim() ?? string.Empty,
            IconText = iconText?.Trim() ?? string.Empty,
            TodayCount = 0,
            LastRecordTime = null,
            ReminderThresholdMinutes = reminderThresholdMinutes,
            UpdatedAt = DateTime.Now
        });

        Save();
        return true;
    }

    public bool IncrementRecordByName(string name, string description, string iconText)
    {
        // 右键菜单的 +1 会调用这里。
        // 如果记录项不存在，就自动创建默认项，再更新次数和最后记录时间。
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var now = DateTime.Now;
        var item = Records.FirstOrDefault(r => r.Name == name);
        if (item == null)
        {
            item = new DailyRecordItem
            {
                Name = name,
                Description = description,
                IconText = iconText,
                TodayCount = 0,
                LastRecordTime = null,
                ReminderThresholdMinutes = GetDefaultReminderThresholdMinutes(name),
                UpdatedAt = null
            };
            Records.Add(item);
        }

        // 今天第一次记录时从 1 开始；同一天后续点击则累加。
        if (item.LastRecordTime is null || item.LastRecordTime.Value.Date != DateTime.Today)
        {
            item.TodayCount = 1;
        }
        else
        {
            item.TodayCount += 1;
        }

        item.LastRecordTime = now;
        item.UpdatedAt = now;
        return Save();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static int? GetDefaultReminderThresholdMinutes(string name)
    {
        // 默认阈值集中在这里，菜单展示和自动创建记录时都能复用。
        return name switch
        {
            "喝水" => 60,
            "休息眼睛" => 45,
            "起身活动" => 90,
            _ => null
        };
    }

    private void AddRecord()
    {
        if (AddRecordByName(NewRecordName, NewRecordDescription, NewRecordIcon))
        {
            ClearNewForm();
        }
    }

    private void ClearNewForm()
    {
        NewRecordName = string.Empty;
        NewRecordDescription = string.Empty;
        NewRecordIcon = string.Empty;
    }

    private void IncrementRecord(DailyRecordItem item)
    {
        if (item == null) return;
        var now = DateTime.Now;
        if (item.LastRecordTime is null || item.LastRecordTime.Value.Date != DateTime.Today)
        {
            item.TodayCount = 1;
        }
        else
        {
            item.TodayCount += 1;
        }
        item.LastRecordTime = now;
        item.UpdatedAt = now;
        Save();
    }

    private bool EnsureDefaultRecord(
        string name,
        string description,
        string iconText,
        int reminderThresholdMinutes)
    {
        // 兼容旧数据：如果默认记录已经存在但没有提醒阈值，就补上阈值并保存。
        var record = Records.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.Ordinal));
        if (record == null)
        {
            Records.Add(new DailyRecordItem
            {
                Name = name,
                Description = description,
                IconText = iconText,
                TodayCount = 0,
                LastRecordTime = null,
                ReminderThresholdMinutes = reminderThresholdMinutes,
                UpdatedAt = null
            });
            return true;
        }

        if (record.ReminderThresholdMinutes is null)
        {
            record.ReminderThresholdMinutes = reminderThresholdMinutes;
            return true;
        }

        return false;
    }

    private bool Save()
    {
        try
        {
            _storage.Save(Records);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Predicate<object?> _canExecute;
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute, Predicate<object?> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
