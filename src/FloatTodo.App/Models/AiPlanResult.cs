using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FloatTodo.App.Models;

/// <summary>
/// Represents the structured plan returned by the AI according to the required schema.
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
}
