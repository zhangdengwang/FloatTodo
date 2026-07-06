using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FloatTodo.App.Models;

/// <summary>
/// Represents a simple in-memory todo item used by the floating shell.
/// </summary>
public sealed class TaskItem : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _description = string.Empty;
    private TaskPriority _priority = TaskPriority.Normal;
    private TaskStatus _status = TaskStatus.Todo;
    private DateTime? _dueTime;
    private DateTime? _completedAt;
    private bool _isProject;
    private string? _parentId;

    public Guid Id { get; set; } = Guid.NewGuid();

    public bool IsProject
    {
        get => _isProject;
        set
        {
            if (_isProject != value)
            {
                _isProject = value;
                OnPropertyChanged();
            }
        }
    }

    public string? ParentId
    {
        get => _parentId;
        set
        {
            if (_parentId != value)
            {
                _parentId = value;
                OnPropertyChanged();
            }
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value;
                OnPropertyChanged();
            }
        }
    }

    public TaskPriority Priority
    {
        get => _priority;
        set
        {
            if (_priority != value)
            {
                _priority = value;
                OnPropertyChanged();
            }
        }
    }

    public TaskStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? DueTime
    {
        get => _dueTime;
        set
        {
            if (_dueTime != value)
            {
                _dueTime = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public DateTime? CompletedAt
    {
        get => _completedAt;
        set
        {
            if (_completedAt != value)
            {
                _completedAt = value;
                OnPropertyChanged();
            }
        }
    }

    // Project-related metadata (optional)
    private string _projectId = string.Empty;
    private string _projectName = string.Empty;
    private string _phase = string.Empty;
    private int _estimatedMinutes;

    public string ProjectId
    {
        get => _projectId;
        set
        {
            if (_projectId != value)
            {
                _projectId = value;
                OnPropertyChanged();
            }
        }
    }

    public string ProjectName
    {
        get => _projectName;
        set
        {
            if (_projectName != value)
            {
                _projectName = value;
                OnPropertyChanged();
            }
        }
    }

    public string Phase
    {
        get => _phase;
        set
        {
            if (_phase != value)
            {
                _phase = value;
                OnPropertyChanged();
            }
        }
    }

    public int EstimatedMinutes
    {
        get => _estimatedMinutes;
        set
        {
            if (_estimatedMinutes != value)
            {
                _estimatedMinutes = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum TaskPriority
{
    Urgent,
    Important,
    Normal
}

public enum TaskStatus
{
    Todo,
    Doing,
    Done
}
