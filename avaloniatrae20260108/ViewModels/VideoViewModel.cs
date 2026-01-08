using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Contentful.Core;
using Contentful.Core.Configuration;
using Contentful.Core.Models;
using System.Collections.ObjectModel;
using avaloniatrae20260108.Models;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Linq;

namespace avaloniatrae20260108.ViewModels;

public partial class VideoViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<Video> Videos { get; } = new();

    public VideoViewModel()
    {
        LoadVideosCommand.Execute(null);
    }

    [RelayCommand]
    public async Task LoadVideos()
    {
        IsLoading = true;
        StatusMessage = "正在載入影片...";
        Videos.Clear();

        try
        {
            // 1. Load from Contentful
            try 
            {
                var settings = LoadSettings();
                if (!string.IsNullOrEmpty(settings.SpaceId) && !string.IsNullOrEmpty(settings.AccessToken))
                {
                    var options = new ContentfulOptions
                    {
                        DeliveryApiKey = settings.AccessToken,
                        SpaceId = settings.SpaceId
                    };

                    var client = new ContentfulClient(new HttpClient(), options);
                    var entries = await client.GetEntries<Video>(queryString: "?content_type=video");

                    if (entries.Any())
                    {
                        foreach (var item in entries)
                        {
                            Videos.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log Contentful error but continue to local
                System.Diagnostics.Debug.WriteLine($"Contentful load error: {ex.Message}");
            }

            // 2. Load local videos
            LoadLocalVideos();

            if (Videos.Any())
            {
                StatusMessage = $"成功載入 {Videos.Count} 部影片";
            }
            else
            {
                StatusMessage = "找不到影片資料。\n請確認 Contentful 上是否有 'video' 類型的內容，\n或是將影片檔案放入應用程式目錄下的 'videos' 資料夾中。";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"載入失敗: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadLocalVideos()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var videosDir = System.IO.Path.Combine(baseDir, "videos");

            if (System.IO.Directory.Exists(videosDir))
            {
                var files = System.IO.Directory.GetFiles(videosDir, "*.*")
                    .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || 
                                f.EndsWith(".avi", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".mov", StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    var fileInfo = new System.IO.FileInfo(file);
                    Videos.Add(new Video
                    {
                        Title = System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name),
                        Description = "本機影片",
                        YoutubeUrl = fileInfo.FullName,
                        PublishDate = fileInfo.CreationTime.ToString("yyyy-MM-dd"),
                        Sys = new SystemProperties { Id = Guid.NewGuid().ToString() }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Local video load error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void OpenVideo(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            // Open in-app player
            var playerWindow = new Views.PlayerWindow(url);
            playerWindow.Show();
        }
        catch (Exception ex)
        {
            StatusMessage = $"無法開啟影片: {ex.Message}";
        }
    }

    private AppSettings LoadSettings()
    {
        if (System.IO.File.Exists("settings.json"))
        {
            try
            {
                var json = System.IO.File.ReadAllText("settings.json");
                return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
        return new AppSettings();
    }
}
