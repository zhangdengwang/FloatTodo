using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FloatTodo.App.Models;

/// <summary>
/// 日常记录模型，例如喝水、休息眼睛、起身活动。
/// 每一项记录当天次数、最后记录时间和提醒阈值，用于右键菜单快捷 +1 与桌宠提醒文字。
/// </summary>
public sealed class DailyRecordItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _iconText = string.Empty;
    private int _todayCount;
    private DateTime? _lastRecordTime;
    private int? _reminderThresholdMinutes;
    private DateTime _createdAt = DateTime.Now;
    private DateTime? _updatedAt;

    /// <summary>
    /// 日常记录唯一标识，用于后续扩展编辑/删除时稳定识别记录项。
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    public string Description
    {
        get => _description;
        set { if (_description != value) { _description = value; OnPropertyChanged(); } }
    }

    public string IconText
    {
        get => _iconText;
        set { if (_iconText != value) { _iconText = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// 今天已经记录的次数。
    /// 如果最后记录时间不是今天，加载时会重置为 0，避免跨天累计误导用户。
    /// </summary>
    public int TodayCount
    {
        get => _todayCount;
        set { if (_todayCount != value) { _todayCount = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// 最后一次点击 +1 的时间。
    /// 为空表示从未记录；日常提醒会根据它和提醒阈值判断是否该提示用户。
    /// </summary>
    public DateTime? LastRecordTime
    {
        get => _lastRecordTime;
        set { if (_lastRecordTime != value) { _lastRecordTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeSinceLastRecord)); } }
    }

    /// <summary>
    /// 提醒阈值，单位为分钟。
    /// 例如喝水默认 60 分钟：超过阈值未记录时，桌宠会显示“请喝水”一类提示。
    /// </summary>
    public int? ReminderThresholdMinutes
    {
        get => _reminderThresholdMinutes;
        set
        {
            if (_reminderThresholdMinutes != value)
            {
                _reminderThresholdMinutes = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        init => _createdAt = value;
    }

    public DateTime? UpdatedAt
    {
        get => _updatedAt;
        set { if (_updatedAt != value) { _updatedAt = value; OnPropertyChanged(); } }
    }

    // 面向界面显示的“距离上次记录多久”文本。
    // 它由 LastRecordTime 动态计算，不单独保存到 JSON，避免数据冗余。
    public string TimeSinceLastRecord
    {
        get
        {
            if (LastRecordTime is null) return "未记录";
            var span = DateTime.Now - LastRecordTime.Value;
            if (span.TotalDays >= 1) return $"{(int)span.TotalDays} 天前";
            if (span.TotalHours >= 1) return $"{(int)span.TotalHours} 小时前";
            if (span.TotalMinutes >= 1) return $"{(int)span.TotalMinutes} 分钟前";
            return "刚刚";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
