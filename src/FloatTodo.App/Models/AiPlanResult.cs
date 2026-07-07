using System.Collections.Generic;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FloatTodo.App.Models;

/// <summary>
/// AI 拆解接口返回的结构化项目计划。
/// 服务层会要求模型按固定 JSON 结构返回，便于程序稳定解析为候选任务。
/// </summary>
public sealed class AiPlanResult
{
    [JsonPropertyName("project_title")]
    public string ProjectTitle { get; set; } = string.Empty;

    [JsonPropertyName("project_summary")]
    public string ProjectSummary { get; set; } = string.Empty;

    [JsonPropertyName("tasks")]
    public List<AiPlanTask> Tasks { get; set; } = new List<AiPlanTask>();

    [JsonPropertyName("risks")]
    public List<string> Risks { get; set; } = new List<string>();
}

public sealed class AiPlanTask
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("phase")]
    public string Phase { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("estimated_minutes")]
    public int EstimatedMinutes { get; set; }

    [JsonPropertyName("suggested_order")]
    public int SuggestedOrder { get; set; }

    [JsonPropertyName("dueTime")]
    [JsonConverter(typeof(FlexibleNullableDateTimeConverter))]
    public DateTime? DueTime { get; set; }
}

/// <summary>
/// AI 返回的截止时间可能是 null、空字符串、yyyy-MM-dd HH:mm 或 ISO 时间。
/// 这里集中做宽松解析，解析失败时返回 null，避免单个时间格式问题导致整次 AI 拆解失败。
/// </summary>
public sealed class FlexibleNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly string[] SupportedFormats =
    [
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm",
        "yyyy/MM/dd HH:mm",
        "yyyy/MM/dd HH:mm:ss"
    ];

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            return null;
        }

        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text.Trim();
        if (DateTime.TryParseExact(text, SupportedFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var exact))
        {
            return exact;
        }

        return DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed
            : null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd HH:mm"));
            return;
        }

        writer.WriteNullValue();
    }
}
