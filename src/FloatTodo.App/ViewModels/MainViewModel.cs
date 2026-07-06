using System.Collections.ObjectModel;
using FloatTodo.App.Models;

namespace FloatTodo.App.ViewModels;

/// <summary>
/// View model for the main floating window shell.
/// </summary>
public sealed class MainViewModel
{
    public ObservableCollection<AppSection> Sections { get; } =
    [
        new AppSection
        {
            Title = "今日任务",
            Description = "待办内容占位区域"
        },
        new AppSection
        {
            Title = "喝水记录",
            Description = "喝水统计占位区域"
        },
        new AppSection
        {
            Title = "AI 拆解项目",
            Description = "AI 生成计划占位区域"
        }
    ];
}
