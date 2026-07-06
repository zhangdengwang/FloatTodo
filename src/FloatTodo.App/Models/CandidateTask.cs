using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FloatTodo.App.Models;

/// <summary>
/// Represents a candidate task produced by the AI planner (mock).
/// </summary>
public sealed class CandidateTask : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isAdded;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public int EstimatedMinutes { get; set; }
    public string Phase { get; set; } = string.Empty;
    public int SuggestedOrder { get; set; }

    /// <summary>
    /// Whether the user has checked this candidate for adding to the todo list.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Whether this candidate has already been added to the main todo list.
    /// </summary>
    public bool IsAdded
    {
        get => _isAdded;
        set
        {
            if (_isAdded != value)
            {
                _isAdded = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
