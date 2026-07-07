using System;
using System.IO;
using System.Text.Json;
using FloatTodo.App.Models;

namespace FloatTodo.App.Services;

/// <summary>
/// DeepSeek API 设置读写服务。
/// API Key 优先从本地配置读取，也支持环境变量，便于开发机和演示机使用不同密钥。
/// </summary>
public sealed class ApiSettingsService
{
    // 本地配置文件属于私人数据，已经在 .gitignore 和打包脚本中排除。
    private const string SettingsPath = "data/local-settings.json";

    public ApiSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var content = File.ReadAllText(SettingsPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var value = JsonSerializer.Deserialize<ApiSettings>(content, options);
                if (value is not null)
                {
                    return value;
                }
            }

            // 如果本地配置不存在，尝试读取环境变量。
            // 这种方式适合不想把 Key 写入项目目录的用户。
            var envKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                return new ApiSettings
                {
                    ApiKey = envKey.Trim(),
                    UpdatedAt = DateTime.Now
                };
            }
        }
        catch
        {
            // 设置读取失败不应影响普通任务、日常记录等离线功能。
        }

        return new ApiSettings();
    }

    public void Save(ApiSettings settings)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath) ?? string.Empty);
            settings.UpdatedAt = DateTime.Now;
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // 保存失败时不抛出到全局，调用方可以通过状态文字或消息框提示用户。
        }
    }
}
