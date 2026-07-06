using System;
using System.Text.Json.Serialization;

namespace FloatTodo.App.Models;

/// <summary>
/// Represents local API settings for DeepSeek integration.
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
