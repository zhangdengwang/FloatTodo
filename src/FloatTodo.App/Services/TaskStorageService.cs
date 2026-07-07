using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FloatTodo.App.Models;

namespace FloatTodo.App.Services;

/// <summary>
/// 任务本地存储服务。
/// 所有普通任务、项目父节点和项目小任务都保存到同一个 tasks.json，避免出现多套任务数据源。
/// </summary>
public sealed class TaskStorageService
{
    private readonly string _dataFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public TaskStorageService(string? basePath = null)
    {
        // 默认把 data 目录放在程序运行目录旁边。
        // 这样源码运行和发布包运行都能用同一套“应用目录/data/tasks.json”规则。
        var root = basePath ?? AppContext.BaseDirectory;
        var dataDir = Path.Combine(root, "data");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        _dataFilePath = Path.Combine(dataDir, "tasks.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    /// <summary>
    /// 从 JSON 文件读取任务列表。
    /// 如果文件不存在、为空或解析失败，返回空列表，避免桌宠启动时因为本地数据损坏而崩溃。
    /// </summary>
    public List<TaskItem> Load()
    {
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                return new List<TaskItem>();
            }

            var json = File.ReadAllText(_dataFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<TaskItem>();
            }

            // 早期版本的任务数据可能没有 Id。
            // 项目/小任务关系依赖 Id，因此加载时检测并补齐，再尽量写回文件完成兼容升级。
            var requiresIdUpgrade = ContainsTaskWithoutId(json);
            var items = JsonSerializer.Deserialize<List<TaskItem>>(json, _jsonOptions);
            var result = items ?? new List<TaskItem>();
            foreach (var task in result.Where(task => task.Id == Guid.Empty))
            {
                task.Id = Guid.NewGuid();
                requiresIdUpgrade = true;
            }

            if (requiresIdUpgrade)
            {
                try
                {
                    Save(result);
                }
                catch
                {
                    // The loaded data remains usable even if the compatibility write-back fails.
                }
            }

            return result;
        }
        catch (Exception)
        {
            // If reading/parsing fails, do not crash the app. Return empty list.
            // Optionally log the exception to a file or telemetry in future iterations.
            return new List<TaskItem>();
        }
    }

    /// <summary>
    /// 将任务列表保存到 JSON 文件。
    /// 保存异常交给调用方决定是否提示用户，因为不同入口的交互方式不同。
    /// </summary>
    public void Save(IEnumerable<TaskItem> tasks)
    {
        // 保存前再次确保目录存在，防止用户删除 data 目录后写入失败。
        var dir = Path.GetDirectoryName(_dataFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var list = tasks.Select(t => t).ToList();
        var json = JsonSerializer.Serialize(list, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    /// <summary>
    /// 将指定任务标记为完成并保存。
    /// 快捷列表和项目小任务列表都复用这里，避免在窗口里复制 JSON 读写逻辑。
    /// </summary>
    public bool MarkCompleted(Guid taskId)
    {
        var tasks = Load();
        var task = tasks.FirstOrDefault(item => item.Id == taskId && !item.IsProject);
        if (task == null)
        {
            return false;
        }

        task.Status = FloatTodo.App.Models.TaskStatus.Done;
        task.CompletedAt = DateTime.Now;
        Save(tasks);
        return true;
    }

    /// <summary>
    /// 删除指定普通任务或项目小任务并保存。
    /// 本方法不会删除项目父节点，也不会级联删除其他小任务。
    /// </summary>
    public bool DeleteTask(Guid taskId)
    {
        var tasks = Load();
        var task = tasks.FirstOrDefault(item => item.Id == taskId && !item.IsProject);
        if (task == null)
        {
            return false;
        }

        tasks.Remove(task);
        Save(tasks);
        return true;
    }

    private static bool ContainsTaskWithoutId(string json)
    {
        // 这里直接检查原始 JSON 属性，而不是先反序列化。
        // 这样可以判断“旧数据缺少 Id”和“Id 值为空”这两种不同场景。
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var taskElement in document.RootElement.EnumerateArray())
        {
            if (taskElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var hasId = taskElement.EnumerateObject()
                .Any(property => string.Equals(property.Name, nameof(TaskItem.Id), StringComparison.OrdinalIgnoreCase));
            if (!hasId)
            {
                return true;
            }
        }

        return false;
    }
}
