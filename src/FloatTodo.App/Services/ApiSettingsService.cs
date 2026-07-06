using System;
using System.IO;
using System.Text.Json;
using FloatTodo.App.Models;

namespace FloatTodo.App.Services;

/// <summary>
/// Reads and writes local API settings for DeepSeek integration.
/// </summary>
public sealed class ApiSettingsService
{
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
            // Swallow errors to keep app running with default settings.
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
            // Fail silently; caller may display a message.
        }
    }
}
