using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FloatTodo.App.Models;
using FloatTodo.App.Services;

namespace FloatTodo.App.ViewModels;

/// <summary>
/// ViewModel for DeepSeek API settings management.
/// </summary>
public sealed class ApiSettingsViewModel : INotifyPropertyChanged
{
    private readonly ApiSettingsService _settingsService;
    private string _apiKey = string.Empty;
    private string _statusText = "未配置 DeepSeek API Key。";
    private bool _isTesting;
    private bool _isKeyVisible;

    public ApiSettingsViewModel(ApiSettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        SaveCommand = new RelayCommand(_ => Save(), _ => !IsTesting);
        TestConnectionCommand = new RelayCommand(_ => { _ = TestConnectionAsync(); }, _ => !IsTesting);
        ToggleKeyVisibilityCommand = new RelayCommand(_ => ToggleKeyVisibility(), _ => !IsTesting);

        Load();
    }

    public string Provider => "DeepSeek";
    public string BaseUrl => "https://api.deepseek.com/chat/completions";
    public string Model => "deepseek-chat";

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            if (_apiKey != value)
            {
                _apiKey = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaskedApiKey));
                UpdateStatusText();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsTesting
    {
        get => _isTesting;
        private set
        {
            if (_isTesting != value)
            {
                _isTesting = value;
                OnPropertyChanged();
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (TestConnectionCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsKeyVisible
    {
        get => _isKeyVisible;
        set
        {
            if (_isKeyVisible != value)
            {
                _isKeyVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaskedApiKey));
            }
        }
    }

    public string MaskedApiKey => IsKeyVisible ? ApiKey : MaskKey(ApiKey);

    public ICommand SaveCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand ToggleKeyVisibilityCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Load()
    {
        var settings = _settingsService.Load();
        ApiKey = settings.ApiKey ?? string.Empty;
        UpdateStatusText();
    }

    public void Save()
    {
        var settings = new ApiSettings
        {
            Provider = Provider,
            ApiKey = ApiKey.Trim(),
            BaseUrl = BaseUrl,
            Model = Model,
            UpdatedAt = DateTime.Now
        };

        _settingsService.Save(settings);
        StatusText = "API Key 已保存到本地。";
    }

    public async Task<TestConnectionResult> TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusText = "未检测到 DeepSeek API Key，请先在 API 设置中配置。";
            return new TestConnectionResult(false, StatusText);
        }

        IsTesting = true;

        try
        {
            using var handler = new HttpClientHandler();
            ConfigureProxy(handler);
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey.Trim());
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                model = Model,
                messages = new[] { new { role = "user", content = "请只返回 OK" } },
                temperature = 0
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(BaseUrl, content).ConfigureAwait(true);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);

            if (response.IsSuccessStatusCode)
            {
                StatusText = "DeepSeek API 连接成功。";
                return new TestConnectionResult(true, StatusText);
            }

            var summary = body.Length > 200 ? body[..200] + "..." : body;
            StatusText = $"连接失败: {response.StatusCode}, {summary}";
            return new TestConnectionResult(false, StatusText);
        }
        catch (Exception ex)
        {
            StatusText = $"连接失败: {ex.Message}";
            return new TestConnectionResult(false, StatusText);
        }
        finally
        {
            IsTesting = false;
        }
    }

    private void UpdateStatusText()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusText = "未配置 DeepSeek API Key。";
        }
        else
        {
            StatusText = $"已配置：{MaskedApiKey}";
        }
    }

    private void ToggleKeyVisibility()
    {
        IsKeyVisible = !IsKeyVisible;
    }

    private static string MaskKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (key.Length <= 8)
            return new string('*', key.Length);

        return key[..4] + new string('*', Math.Max(0, key.Length - 8)) + key[^4..];
    }

    private static void ConfigureProxy(HttpClientHandler handler)
    {
        var proxyUrl = Environment.GetEnvironmentVariable("HTTPS_PROXY") ?? Environment.GetEnvironmentVariable("HTTP_PROXY");
        if (string.IsNullOrWhiteSpace(proxyUrl))
            return;

        try
        {
            var proxy = new System.Net.WebProxy(proxyUrl)
            {
                BypassProxyOnLocal = true
            };
            handler.Proxy = proxy;
            handler.UseProxy = true;
        }
        catch
        {
            // Ignore invalid proxy settings and continue without proxy.
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public sealed class TestConnectionResult
{
    public TestConnectionResult(bool successful, string message)
    {
        Successful = successful;
        Message = message;
    }

    public bool Successful { get; }
    public string Message { get; }
}
