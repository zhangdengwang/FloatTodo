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
/// AI 拆解 ViewModel。
/// 负责把用户输入发送给 AI 服务，接收候选任务，并在用户确认后创建项目和小任务。
/// </summary>
public sealed class AiPlannerViewModel : INotifyPropertyChanged
{
    private readonly MainViewModel _main;
    private readonly AiPlannerService _service;
    private string _projectName = string.Empty;
    private string _projectDescription = string.Empty;
    private bool _isPlanning;
    private string _currentBatchProjectName = string.Empty;

    public AiPlannerViewModel(MainViewModel main, AiPlannerService service)
    {
        _main = main ?? throw new ArgumentNullException(nameof(main));
        _service = service ?? throw new ArgumentNullException(nameof(service));

        Candidates = new ObservableCollection<CandidateTask>();

        GenerateCommand = new RelayCommand(_ => { _ = GenerateAsync(); }, _ => !IsPlanning);
        // “加入选中任务”按钮保持可点击，由命令内部负责提示“还没拆解”或“未选择任务”。
        // 这样按钮始终可见，用户能明确知道下一步操作在哪里。
        AddSelectedToTasksCommand = new RelayCommand(_ => AddSelectedToTasks(), _ => true);

        Candidates.CollectionChanged += (s, e) => AttachCandidateHandlers();
    }

    public ObservableCollection<CandidateTask> Candidates { get; }

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

            // 项目名优先使用用户输入；如果用户没填，则使用 AI 返回标题；再不行才从描述截取。
            // 这样可以保证后续创建项目父节点时一定有一个可显示名称。
            var title = !string.IsNullOrWhiteSpace(ProjectName)
                ? ProjectName.Trim()
                : !string.IsNullOrWhiteSpace(plan.ProjectTitle)
                    ? plan.ProjectTitle.Trim()
                    : (ProjectDescription.Length <= 20 ? ProjectDescription.Trim() : ProjectDescription[..20].Trim());

            _currentBatchProjectName = title;

            // 将 AI 返回的小任务转换为候选任务。
            // 此时只进入候选列表，不立即写入任务 JSON，避免把用户不想要的 AI 输出保存下来。
            foreach (var t in plan.Tasks)
            {
                var description = string.IsNullOrWhiteSpace(t.Description)
                    ? $"完成“{t.Title}”相关工作，并补充必要步骤、要求和验收结果。"
                    : t.Description.Trim();
                var candidate = new CandidateTask
                {
                    Title = t.Title,
                    Description = description,
                    Phase = t.Phase,
                    EstimatedMinutes = t.EstimatedMinutes,
                    SuggestedOrder = t.SuggestedOrder,
                    Priority = ParsePriority(t.Priority),
                    DueTime = t.DueTime
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
            MessageBox.Show("请先拆解项目", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var selected = Candidates.Where(c => c.IsSelected && !c.IsAdded).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("请选择要加入的任务", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var projectName = !string.IsNullOrWhiteSpace(ProjectName)
            ? ProjectName.Trim()
            : _currentBatchProjectName;
        if (string.IsNullOrWhiteSpace(projectName))
        {
            projectName = ProjectDescription.Length <= 20
                ? ProjectDescription.Trim()
                : ProjectDescription[..20].Trim();
        }

        // 保存时先创建一个项目父节点，再把选中的候选任务作为子任务挂到该项目下。
        // 这样项目进度可以通过 ParentId 自动统计，不需要新增项目 JSON。
        var project = new TaskItem
        {
            Title = projectName,
            Description = ProjectDescription.Trim(),
            Priority = TaskPriority.Important,
            Status = FloatTodo.App.Models.TaskStatus.Todo,
            CreatedAt = DateTime.Now,
            DueTime = null,
            IsProject = true,
            ParentId = null,
            ProjectName = projectName
        };
        _main.AddTaskItem(project);
        var projectId = project.Id.ToString();

        foreach (var c in selected)
        {
            // 每个候选任务转换成一个真实小任务，并记录项目名、阶段和预估分钟数用于展示。
            var task = new TaskItem
            {
                Title = c.Title,
                Description = c.Description,
                Priority = c.Priority,
                Status = FloatTodo.App.Models.TaskStatus.Todo,
                CreatedAt = DateTime.Now,
                DueTime = c.DueTime,
                IsProject = false,
                ParentId = projectId,
                ProjectId = projectId,
                ProjectName = projectName,
                Phase = c.Phase,
                EstimatedMinutes = c.EstimatedMinutes
            };

            _main.AddTaskItem(task);
            c.IsAdded = true;
            c.IsSelected = false;
        }

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
        // AI 返回的是字符串，这里转换为程序内部枚举；无法识别时按普通优先级处理。
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
