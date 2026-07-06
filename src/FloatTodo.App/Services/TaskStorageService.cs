using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FloatTodo.App.Models;

namespace FloatTodo.App.Services;

/// <summary>
/// Provides simple JSON file persistence for TaskItem objects.
/// </summary>
public sealed class TaskStorageService
{
    private readonly string _dataFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public TaskStorageService(string? basePath = null)
    {
        // Default to application base directory so the data folder is next to the app.
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
    /// Loads tasks from the JSON file. Returns empty list on any error.
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
    /// Saves tasks to the JSON file. Exceptions are propagated to caller.
    /// </summary>
    public void Save(IEnumerable<TaskItem> tasks)
    {
        // Ensure folder exists (defensive)
        var dir = Path.GetDirectoryName(_dataFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var list = tasks.Select(t => t).ToList();
        var json = JsonSerializer.Serialize(list, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private static bool ContainsTaskWithoutId(string json)
    {
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
