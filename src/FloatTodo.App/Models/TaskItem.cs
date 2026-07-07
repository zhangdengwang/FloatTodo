using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FloatTodo.App.Models;

/// <summary>
/// 任务数据模型。
/// 普通任务、项目父节点、项目小任务都复用这一种结构，便于使用同一个 JSON 文件保存。
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

    /// <summary>
    /// 任务唯一标识。
    /// 项目小任务会通过 ParentId 指向项目父节点的 Id，因此旧数据缺 Id 时需要兼容补齐。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 标记当前项是否是“项目父节点”。
    /// 项目父节点用于组织小任务，不直接计入桌宠红点和快截止普通任务统计。
    /// </summary>
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

    /// <summary>
    /// 父任务 Id。
    /// 普通任务和项目父节点通常为空；项目小任务通过它关联到所属项目。
    /// </summary>
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

    /// <summary>
    /// 截止时间。
    /// 为空表示用户没有设置截止时间；桌宠红点只统计 24 小时内截止或已逾期的未完成非项目任务。
    /// </summary>
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

    // 项目相关的可选元数据。
    // 这些字段用于 AI 拆解、项目进度展示和旧界面兼容，不影响普通任务的基础行为。
    private string _projectId = string.Empty;
    private string _projectName = string.Empty;
    private string _phase = string.Empty;
    private int _estimatedMinutes;

    /// <summary>
    /// 项目 Id 字符串。
    /// 对新结构来说 ParentId 是主要关联字段；保留 ProjectId 是为了兼容已有展示和旧数据。
    /// </summary>
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

    /// <summary>
    /// 项目名称冗余字段。
    /// 小任务保存一份项目名，可以在快捷列表或旧界面中不额外查父节点也能显示项目归属。
    /// </summary>
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
