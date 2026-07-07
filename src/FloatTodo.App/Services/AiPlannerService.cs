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
/// AI 项目拆解服务。
/// 负责读取 DeepSeek 配置、发送项目描述、解析模型返回的 JSON，并把结果转换为内部计划对象。
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

        // API Key 既可以来自本地设置，也可以来自环境变量。
        // 这样课程提交代码时不需要把个人密钥写进仓库。
        var settings = _settingsService.Load();
        var apiKey = GetApiKey(settings);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("未检测到 DeepSeek API Key，请先在 API 设置中配置。");

        using var handler = new HttpClientHandler();
        ConfigureProxy(handler);

        using var http = new HttpClient(handler);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // systemPrompt 明确要求模型只做“项目拆解助手”，降低返回闲聊文本的概率。
        var systemPrompt =
            "你是一个项目任务拆解助手。用户会输入一个项目、作业或任务目标。请严格按照指定的 JSON 模式返回，不要输出多余文字或 Markdown。";

        // userPrompt 中给出固定 JSON 模板，要求每个小任务包含 description。
        // 这样 AI 生成的项目小任务不仅有标题，也能保存到 TaskItem.Description 供详情窗口展示。
        var userPrompt = new StringBuilder();
        userPrompt.AppendLine("请把下列项目拆解成 6 到 12 个可执行小任务。每个任务必须具体且可执行。输出必须是合法 JSON，且遵守下列结构：");
        userPrompt.AppendLine("{\n  \"project_title\": \"...\",\n  \"project_summary\": \"...\",\n  \"tasks\": [ { \"title\": \"...\", \"description\": \"写清楚该小任务的具体步骤、验收标准或注意事项\", \"phase\": \"规划|设计|实现|测试|文档|展示|其他\", \"priority\": \"Urgent|Important|Normal\", \"estimated_minutes\": 60, \"suggested_order\": 1, \"dueTime\": null } ],\n  \"risks\": [ \"...\" ]\n}");
        userPrompt.AppendLine();
        userPrompt.AppendLine("约束：");
        userPrompt.AppendLine("1) 不要输出 Markdown 或解释性文本，2) 只输出 JSON，3) 每个任务必须包含非空 description，4) priority 只能为 Urgent、Important 或 Normal，5) estimated_minutes 必须是整数，6) phase 必须从 规划, 设计, 实现, 测试, 文档, 展示, 其他 中选择，7) suggested_order 从 1 开始递增。\n");
        userPrompt.AppendLine("用户输入：");
        userPrompt.AppendLine(projectDescription);

        // DeepSeek chat/completions 接口的请求体：模型名、消息数组和较低 temperature。
        // temperature 设为 0 是为了让任务拆解结果更稳定，便于演示和解析。
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

        HttpResponseMessage resp;
        string respText;
        try
        {
            resp = await http.PostAsync(settings.BaseUrl, content).ConfigureAwait(false);
            respText = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException httpEx)
        {
            throw new Exception($"AI 拆解失败，网络错误: {httpEx.Message}");
        }
        catch (TaskCanceledException tcEx)
        {
            throw new Exception($"AI 拆解失败，网络超时: {tcEx.Message}");
        }

        if (!resp.IsSuccessStatusCode)
        {
            var summary = string.IsNullOrWhiteSpace(respText) ? "(无响应内容)" : (respText.Length > 200 ? respText[..200] + "..." : respText);
            throw new Exception($"AI 拆解失败。HTTP {resp.StatusCode}: {summary}");
        }

        try
        {
            using var doc = JsonDocument.Parse(respText);
            var root = doc.RootElement;
            var choices = root.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
                throw new Exception("AI 返回内容为空。");

            // DeepSeek 返回的真正文本在 choices[0].message.content 中。
            var message = choices[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(message))
                throw new Exception("AI 返回内容为空。");

            // 有些模型会额外包一层 ```json 代码块，这里先清理再解析。
            message = CleanAiResponseContent(message);
            if (string.IsNullOrWhiteSpace(message))
                throw new Exception("AI 返回内容为空。请检查模型输出。\n");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var plan = DeserializePlan(message, options);
            if (plan is null || plan.Tasks is null)
                throw new Exception("AI 返回格式异常，请重新尝试。");

            return plan;
        }
        catch (JsonException)
        {
            throw new Exception("AI 返回格式异常，请重新尝试。");
        }
    }

    private static string CleanAiResponseContent(string content)
    {
        content = content.Trim();
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // 去掉 Markdown 代码块和 json 语言标记，提升对模型输出格式波动的容错。
        if (content.StartsWith("```"))
        {
            var endFence = content.LastIndexOf("```");
            if (endFence > 3)
            {
                content = content[3..endFence].Trim();
            }
        }

        if (content.StartsWith("json", StringComparison.OrdinalIgnoreCase))
        {
            var idx = content.IndexOf('\n');
            if (idx >= 0)
            {
                content = content[(idx + 1)..].Trim();
            }
        }

        var firstBrace = content.IndexOf('{');
        var lastBrace = content.LastIndexOf('}');
        var firstBracket = content.IndexOf('[');
        var lastBracket = content.LastIndexOf(']');

        if (firstBracket >= 0 &&
            lastBracket > firstBracket &&
            (firstBrace < 0 || firstBracket < firstBrace))
        {
            content = content[firstBracket..(lastBracket + 1)].Trim();
        }
        else if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            content = content[firstBrace..(lastBrace + 1)].Trim();
        }

        return content;
    }

    private static AiPlanResult? DeserializePlan(string message, JsonSerializerOptions options)
    {
        if (message.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            var tasks = JsonSerializer.Deserialize<List<AiPlanTask>>(message, options);
            return tasks is null
                ? null
                : new AiPlanResult { Tasks = tasks };
        }

        return JsonSerializer.Deserialize<AiPlanResult>(message, options);
    }

    private static string? GetApiKey(ApiSettings settings)
    {
        // 本地配置优先；环境变量作为兜底，便于不落盘地提供密钥。
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            return settings.ApiKey.Trim();

        var envKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        return string.IsNullOrWhiteSpace(envKey) ? null : envKey.Trim();
    }

    private static void ConfigureProxy(HttpClientHandler handler)
    {
        // 支持常见代理环境变量，方便校园网、公司网或本机代理场景下测试 AI。
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
