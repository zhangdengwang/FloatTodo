using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FloatTodo.App.Models;

namespace FloatTodo.App.Services;

/// <summary>
/// 日常记录本地存储服务。
/// 负责读取和保存 daily-records.json，不新增额外数据文件，保证右键快捷 +1 和查看窗口使用同一份数据。
/// </summary>
public sealed class DailyRecordStorageService
{
    private readonly string _dataFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public DailyRecordStorageService(string? basePath = null)
    {
        // 日常记录跟任务一样放在程序目录下的 data 文件夹中。
        // 发布包首次运行时如果没有 data 目录，会在这里自动创建。
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
        // 调用方传入的是 ObservableCollection 时，先转成 List 再序列化，避免保存过程受集合变更影响。
        var dir = Path.GetDirectoryName(_dataFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var list = items.ToList();
        var json = JsonSerializer.Serialize(list, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }
}
