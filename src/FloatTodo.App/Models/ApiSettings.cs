using System;
using System.Text.Json.Serialization;

namespace FloatTodo.App.Models;

/// <summary>
/// DeepSeek API 本地配置。
/// API Key 属于个人密钥，只保存在本地配置文件或环境变量中，不应该提交到 Git。
/// </summary>
public sealed class ApiSettings
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "DeepSeek";

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = "https://api.deepseek.com/chat/completions";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "deepseek-chat";

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
