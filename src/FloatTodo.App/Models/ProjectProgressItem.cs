using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FloatTodo.App.Models;

public sealed class ProjectProgressItem : INotifyPropertyChanged
{
    private string _projectId = string.Empty;
    private string _projectName = string.Empty;
    private int _total;
    private int _completed;

    public string ProjectId
    {
        get => _projectId;
        set { if (_projectId != value) { _projectId = value; OnPropertyChanged(); } }
    }

    public string ProjectName
    {
        get => _projectName;
        set { if (_projectName != value) { _projectName = value; OnPropertyChanged(); } }
    }

    public int Total
    {
        get => _total;
        set { if (_total != value) { _total = value; OnPropertyChanged(); OnPropertyChanged(nameof(Percentage)); } }
    }

    public int Completed
    {
        get => _completed;
        set { if (_completed != value) { _completed = value; OnPropertyChanged(); OnPropertyChanged(nameof(Percentage)); } }
    }

    public int Percentage => Total == 0 ? 0 : (int)Math.Round((double)Completed / Total * 100);

    // Display-friendly progress text, e.g. "贪吃蛇小游戏：已完成 2 / 8 项，进度 25%"
    public string DisplayProgressText => string.IsNullOrWhiteSpace(ProjectName)
        ? $"已完成 {Completed} / {Total} 项，进度 {Percentage}%"
        : $"{ProjectName}：已完成 {Completed} / {Total} 项，进度 {Percentage}%";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
