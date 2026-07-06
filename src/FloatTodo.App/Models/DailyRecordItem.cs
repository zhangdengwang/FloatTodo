using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FloatTodo.App.Models;

/// <summary>
/// Represents a daily routine record item (e.g., drinking water, stretching).
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

    public int TodayCount
    {
        get => _todayCount;
        set { if (_todayCount != value) { _todayCount = value; OnPropertyChanged(); } }
    }

    public DateTime? LastRecordTime
    {
        get => _lastRecordTime;
        set { if (_lastRecordTime != value) { _lastRecordTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeSinceLastRecord)); } }
    }

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

    // Human readable time since last record
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
