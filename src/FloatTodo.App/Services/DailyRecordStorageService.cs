using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FloatTodo.App.Models;

namespace FloatTodo.App.Services;

/// <summary>
/// Storage service for daily routine records persisted to JSON.
/// </summary>
public sealed class DailyRecordStorageService
{
    private readonly string _dataFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public DailyRecordStorageService(string? basePath = null)
    {
        var root = basePath ?? AppContext.BaseDirectory;
        var dataDir = Path.Combine(root, "data");
        if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);

        _dataFilePath = Path.Combine(dataDir, "daily-records.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public List<DailyRecordItem> Load()
    {
        try
        {
            if (!File.Exists(_dataFilePath)) return new List<DailyRecordItem>();
            var json = File.ReadAllText(_dataFilePath);
            if (string.IsNullOrWhiteSpace(json)) return new List<DailyRecordItem>();
            var items = JsonSerializer.Deserialize<List<DailyRecordItem>>(json, _jsonOptions);
            return items ?? new List<DailyRecordItem>();
        }
        catch (Exception)
        {
            // Reading/parsing failed — return empty list to avoid crashing.
            return new List<DailyRecordItem>();
        }
    }

    public void Save(IEnumerable<DailyRecordItem> items)
    {
        var dir = Path.GetDirectoryName(_dataFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var list = items.ToList();
        var json = JsonSerializer.Serialize(list, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }
}
