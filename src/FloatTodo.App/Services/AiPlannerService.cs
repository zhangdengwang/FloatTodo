using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FloatTodo.App.Models;

namespace FloatTodo.App.Services;

/// <summary>
/// AI planner service that calls DeepSeek using the configured API settings.
/// It prefers locally saved settings and falls back to environment variable when needed.
/// </summary>
public sealed class AiPlannerService
{
    private readonly ApiSettingsService _settingsService;

    public AiPlannerService(ApiSettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    public async Task<AiPlanResult> PlanProjectAsync(string projectDescription)
    {
        if (string.IsNullOrWhiteSpace(projectDescription))
            throw new ArgumentException("请先输入项目描述。", nameof(projectDescription));

        var settings = _settingsService.Load();
        var apiKey = GetApiKey(settings);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("未检测到 DeepSeek API Key，请先在 API 设置中配置。");

        using var handler = new HttpClientHandler();
        ConfigureProxy(handler);

        using var http = new HttpClient(handler);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var systemPrompt =
            "你是一个项目任务拆解助手。用户会输入一个项目、作业或任务目标。请严格按照指定的 JSON 模式返回，不要输出多余文字或 Markdown。";

        var userPrompt = new StringBuilder();
        userPrompt.AppendLine("请把下列项目拆解成 6 到 12 个可执行小任务。每个任务必须具体且可执行。输出必须是合法 JSON，且遵守下列结构：");
        userPrompt.AppendLine("{\n  \"project_title\": \"...\",\n  \"project_summary\": \"...\",\n  \"tasks\": [ { \"title\": \"...\", \"description\": \"...\", \"phase\": \"规划|设计|实现|测试|文档|展示|其他\", \"priority\": \"Urgent|Important|Normal\", \"estimated_minutes\": 60, \"suggested_order\": 1 } ],\n  \"risks\": [ \"...\" ]\n}");
        userPrompt.AppendLine();
        userPrompt.AppendLine("约束：");
        userPrompt.AppendLine("1) 不要输出 Markdown 或解释性文本，2) 只输出 JSON，3) priority 只能为 Urgent、Important 或 Normal，4) estimated_minutes 必须是整数，5) phase 必须从 规划, 设计, 实现, 测试, 文档, 展示, 其他 中选择，6) suggested_order 从 1 开始递增。\n");
        userPrompt.AppendLine("用户输入：");
        userPrompt.AppendLine(projectDescription);

        var payload = new
        {
            model = settings.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt.ToString() }
            },
            temperature = 0
        };

        var body = JsonSerializer.Serialize(payload);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var resp = await http.PostAsync(settings.BaseUrl, content).ConfigureAwait(false);
        var respText = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            var summary = respText.Length > 200 ? respText[..200] + "..." : respText;
            throw new Exception($"AI 拆解失败，请检查网络或 API Key。HTTP {resp.StatusCode}: {summary}");
        }

        try
        {
            using var doc = JsonDocument.Parse(respText);
            var root = doc.RootElement;
            var choices = root.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
                throw new Exception("AI 返回内容为空。");

            var message = choices[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(message))
                throw new Exception("AI 返回内容为空。");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var plan = JsonSerializer.Deserialize<AiPlanResult>(message, options);
            if (plan is null)
                throw new Exception("AI 返回格式异常，请重新尝试。");

            return plan;
        }
        catch (JsonException)
        {
            throw new Exception("AI 返回格式异常，请重新尝试。");
        }
    }

    private static string? GetApiKey(ApiSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            return settings.ApiKey.Trim();

        var envKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        return string.IsNullOrWhiteSpace(envKey) ? null : envKey.Trim();
    }

    private static void ConfigureProxy(HttpClientHandler handler)
    {
        var proxyUrl = Environment.GetEnvironmentVariable("HTTPS_PROXY") ?? Environment.GetEnvironmentVariable("HTTP_PROXY");
        if (string.IsNullOrWhiteSpace(proxyUrl))
            return;

        try
        {
            handler.Proxy = new WebProxy(proxyUrl) { BypassProxyOnLocal = true };
            handler.UseProxy = true;
        }
        catch
        {
            // Ignore invalid proxies.
        }
    }
}
