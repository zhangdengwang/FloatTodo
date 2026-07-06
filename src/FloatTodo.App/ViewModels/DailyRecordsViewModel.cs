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
/// View model to manage daily routine records. Persists to JSON via DailyRecordStorageService.
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

        // Load existing records or initialize default.
        var items = _storage.Load();
        if (items == null || items.Count == 0)
        {
            var defaultItem = new DailyRecordItem
            {
                Name = "喝水",
                Description = "记录今日喝水次数",
                IconText = "💧",
                TodayCount = 0,
                LastRecordTime = null,
                UpdatedAt = null
            };
            Records.Add(defaultItem);
            Save();
        }
        else
        {
            // Ensure today's counts are reset if last record was not today
            foreach (var r in items)
            {
                if (r.LastRecordTime is null || r.LastRecordTime.Value.Date != DateTime.Today)
                {
                    r.TodayCount = 0;
                }
                Records.Add(r);
            }
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void AddRecord()
    {
        var item = new DailyRecordItem
        {
            Name = NewRecordName.Trim(),
            Description = NewRecordDescription.Trim(),
            IconText = string.IsNullOrEmpty(NewRecordIcon) ? "" : NewRecordIcon.Trim(),
            TodayCount = 0,
            LastRecordTime = null,
            UpdatedAt = DateTime.Now
        };
        Records.Add(item);
        ClearNewForm();
        Save();
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

    private void Save()
    {
        try
        {
            _storage.Save(Records);
        }
        catch
        {
            // Ignore save errors for now to avoid crashing the app. Could log later.
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
