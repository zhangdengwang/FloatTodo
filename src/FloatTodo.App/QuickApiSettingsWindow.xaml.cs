using System;
using System.Windows;
using FloatTodo.App.Services;
using FloatTodo.App.ViewModels;

namespace FloatTodo.App;

/// <summary>
/// 右键菜单打开的轻量 API 设置窗口。
/// 只负责 DeepSeek API Key 的本地保存和连接测试，不影响普通任务、项目和日常记录功能。
/// </summary>
public partial class QuickApiSettingsWindow : Window
{
    private readonly ApiSettingsViewModel _viewModel;

    public QuickApiSettingsWindow()
    {
        InitializeComponent();
        _viewModel = new ApiSettingsViewModel(new ApiSettingsService());
        ApiKeyBox.Password = _viewModel.ApiKey;
        StatusText.Text = _viewModel.StatusText;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ApiKey = ApiKeyBox.Password.Trim();
        _viewModel.Save();
        StatusText.Text = _viewModel.StatusText;
    }

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel.ApiKey = ApiKeyBox.Password.Trim();
            StatusText.Text = "正在测试 DeepSeek 连接...";
            var result = await _viewModel.TestConnectionAsync();
            StatusText.Text = result.Message;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"测试连接失败：{ex.Message}";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
