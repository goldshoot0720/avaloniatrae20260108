using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Contentful.Core;
using Contentful.Core.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace avaloniatrae20260108.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _spaceId = "navontrqk0l3";

    [ObservableProperty]
    private string _accessToken = "83Q5hThGBPCIgXAYX7Fc-gSUN-psxg_j-F-gXSskQBc";

    [ObservableProperty]
    private string _managementToken = string.Empty;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private string _testResultColor = "White";

    [RelayCommand]
    public async Task TestToken()
    {
        TestResult = "測試連線中...";
        TestResultColor = "Yellow";
        
        var sb = new System.Text.StringBuilder();
        bool hasError = false;

        // 1. Test Delivery API (if AccessToken is provided)
        if (!string.IsNullOrWhiteSpace(AccessToken))
        {
            try
            {
                var options = new ContentfulOptions
                {
                    DeliveryApiKey = AccessToken,
                    SpaceId = SpaceId
                };

                var client = new ContentfulClient(new HttpClient(), options);
                var space = await client.GetSpace();
                sb.AppendLine($"Delivery API: 連線成功 ({space.Name})");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Delivery API: 失敗 ({ex.Message})");
                hasError = true;
            }
        }

        // 2. Test Management API (if ManagementToken is provided)
        if (!string.IsNullOrWhiteSpace(ManagementToken))
        {
            try
            {
                var options = new ContentfulOptions
                {
                    ManagementApiKey = ManagementToken,
                    SpaceId = SpaceId
                };

                var client = new ContentfulManagementClient(new HttpClient(), options);
                
                try 
                {
                    var space = await client.GetSpace(SpaceId);
                    sb.AppendLine($"Management API: 連線成功 ({space.Name})");
                }
                catch (Exception ex) when (ex.Message.Contains("404") || ex.Message.Contains("Not Found"))
                {
                    sb.AppendLine($"Management API: Space ID '{SpaceId}' 不存在 (404)。");
                    try 
                    {
                        var spaces = await client.GetSpaces();
                        if (spaces != null)
                        {
                            sb.AppendLine("帳戶內可用的 Spaces:");
                            foreach(var s in spaces)
                            {
                                 sb.AppendLine($"- {s.Name}: {s.SystemProperties.Id}");
                            }
                        }
                    }
                    catch (Exception listEx)
                    {
                         sb.AppendLine($"無法取得 Space 列表: {listEx.Message}");
                    }
                    hasError = true;
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Management API: 失敗 ({ex.Message})");
                hasError = true;
            }
        }

        if (sb.Length == 0)
        {
             TestResult = "請輸入 Token 進行測試";
             TestResultColor = "White";
        }
        else
        {
             TestResult = sb.ToString();
             TestResultColor = hasError ? "Red" : "LightGreen";
        }
    }

    [RelayCommand]
    public void SaveSettings()
    {
        try
        {
            var settings = new AppSettings
            {
                SpaceId = SpaceId,
                AccessToken = AccessToken,
                ManagementToken = ManagementToken
            };

            var json = System.Text.Json.JsonSerializer.Serialize(settings);
            System.IO.File.WriteAllText("settings.json", json);

            TestResult = "設定已儲存！";
            TestResultColor = "LightGreen";
        }
        catch (Exception ex)
        {
            TestResult = $"儲存失敗: {ex.Message}";
            TestResultColor = "Red";
        }
    }

    public SettingsViewModel()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        if (System.IO.File.Exists("settings.json"))
        {
            try
            {
                var json = System.IO.File.ReadAllText("settings.json");
                var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    SpaceId = settings.SpaceId;
                    AccessToken = settings.AccessToken;
                    ManagementToken = settings.ManagementToken;
                }
            }
            catch
            {
                // Ignore load errors
            }
        }
    }
}

public class AppSettings
{
    public string SpaceId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string ManagementToken { get; set; } = string.Empty;
}
