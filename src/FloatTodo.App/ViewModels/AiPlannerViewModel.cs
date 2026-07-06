using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using FloatTodo.App.Models;
using FloatTodo.App.Services;

namespace FloatTodo.App.ViewModels;

/// <summary>
/// ViewModel for the AI planner UI. Produces candidate tasks and allows
/// adding selected candidates into the main todo list.
/// </summary>
public sealed class AiPlannerViewModel : INotifyPropertyChanged
{
    private readonly MainViewModel _main;
    private readonly AiPlannerService _service;
    private string _projectDescription = string.Empty;
    private bool _isPlanning;

    public AiPlannerViewModel(MainViewModel main, AiPlannerService service)
    {
        _main = main ?? throw new ArgumentNullException(nameof(main));
        _service = service ?? throw new ArgumentNullException(nameof(service));

        Candidates = new ObservableCollection<CandidateTask>();

        GenerateCommand = new RelayCommand(_ => { _ = GenerateAsync(); }, _ => !IsPlanning);
        // The Add-to-tasks button should always be clickable; command handles empty/none-selected cases.
        AddSelectedToTasksCommand = new RelayCommand(_ => AddSelectedToTasks(), _ => true);

        Candidates.CollectionChanged += (s, e) => AttachCandidateHandlers();
    }

    public ObservableCollection<CandidateTask> Candidates { get; }

    public string ProjectDescription
    {
        get => _projectDescription;
        set
        {
            if (_projectDescription != value)
            {
                _projectDescription = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand GenerateCommand { get; }

    public ICommand AddSelectedToTasksCommand { get; }

    public bool IsPlanning
    {
        get => _isPlanning;
        private set
        {
            if (_isPlanning != value)
            {
                _isPlanning = value;
                OnPropertyChanged();
                (GenerateCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (AddSelectedToTasksCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async System.Threading.Tasks.Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(ProjectDescription))
        {
            MessageBox.Show("请先输入项目描述。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsPlanning = true;
        Candidates.Clear();

        try
        {
            var plan = await _service.PlanProjectAsync(ProjectDescription).ConfigureAwait(true);

            // Map AI tasks into CandidateTask instances.
            foreach (var t in plan.Tasks)
            {
                var candidate = new CandidateTask
                {
                    Title = t.Title,
                    Description = t.Description,
                    Phase = t.Phase,
                    EstimatedMinutes = t.EstimatedMinutes,
                    SuggestedOrder = t.SuggestedOrder,
                    Priority = ParsePriority(t.Priority)
                };

                Candidates.Add(candidate);
            }

            AttachCandidateHandlers();
            (AddSelectedToTasksCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        catch (InvalidOperationException inv)
        {
            MessageBox.Show(inv.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            // Service throws user-friendly messages for known cases; otherwise show generic.
            var msg = ex.Message ?? "AI 拆解失败，请检查网络或 API Key。";
            MessageBox.Show(msg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsPlanning = false;
        }
    }

    private void AddSelectedToTasks()
    {
        if (!Candidates.Any())
        {
            MessageBox.Show("请先拆解项目。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var selected = Candidates.Where(c => c.IsSelected && !c.IsAdded).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("请先选择要加入的任务。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        foreach (var c in selected)
        {
            var task = new TaskItem
            {
                Title = c.Title,
                Description = c.Description,
                Priority = c.Priority,
                Status = FloatTodo.App.Models.TaskStatus.Todo,
                CreatedAt = DateTime.Now,
                DueTime = null
            };

            _main.AddTaskItem(task);
            c.IsAdded = true;
            c.IsSelected = false;
        }

        var count = selected.Count;
        MessageBox.Show($"已加入 {count} 个任务到待办。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        (AddSelectedToTasksCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void AttachCandidateHandlers()
    {
        foreach (var c in Candidates)
        {
            c.PropertyChanged -= CandidateOnPropertyChanged;
            c.PropertyChanged += CandidateOnPropertyChanged;
        }
    }

    private void CandidateOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CandidateTask.IsSelected) || e.PropertyName == nameof(CandidateTask.IsAdded))
        {
            (AddSelectedToTasksCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private static TaskPriority ParsePriority(string p)
    {
        return p?.Trim() switch
        {
            "Urgent" => TaskPriority.Urgent,
            "Important" => TaskPriority.Important,
            _ => TaskPriority.Normal
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
