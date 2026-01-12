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
using Avalonia.Threading;
using System.Collections.Generic;

namespace avaloniatrae20260108.ViewModels;

public partial class VideoViewModel : ViewModelBase
{
    // 記憶體暫存
    private static List<Video>? _cachedVideos = null;
    private static bool _isFirstLoad = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<Video> Videos { get; } = new();

    public VideoViewModel()
    {
        // 延遲載入，避免阻塞UI
        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(100);
            await LoadVideos();
        });
    }

    [RelayCommand]
    public async Task LoadVideos()
    {
        if (IsLoading) return;

        // 檢查是否有暫存資料
        if (_cachedVideos != null && !_isFirstLoad)
        {
            IsLoading = true;
            StatusMessage = "從記憶體載入影片...";
            
            Videos.Clear();
            foreach (var video in _cachedVideos)
            {
                Videos.Add(video);
            }
            
            StatusMessage = $"已從記憶體載入 {Videos.Count} 部影片";
            IsLoading = false;
            return;
        }

        // 第一次載入
        IsLoading = true;
        ProgressValue = 0;
        Videos.Clear();
        StatusMessage = "正在搜尋並載入影片...";

        try
        {
            var progress = new Progress<double>(p => ProgressValue = p);
            var tempVideos = new List<Video>();
            
            await Task.Run(async () => 
            {
                // 1. Contentful (Optional, simplified progress)
                await Dispatcher.UIThread.InvokeAsync(() => StatusMessage = "正在檢查線上影片...");
                
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
                            tempVideos.AddRange(entries);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Contentful load error: {ex.Message}");
                }

                // 2. Local Videos (with progress)
                await Dispatcher.UIThread.InvokeAsync(() => StatusMessage = "正在搜尋本機影片...");
                await LoadLocalVideosAsync(progress, tempVideos);
            });
            
            // 將資料加入UI並暫存
            foreach (var video in tempVideos)
            {
                Videos.Add(video);
            }
            
            // 暫存到記憶體
            _cachedVideos = new List<Video>(tempVideos);
            _isFirstLoad = false;
            
            if (Videos.Count > 0)
            {
                StatusMessage = $"已載入 {Videos.Count} 部影片 (已暫存至記憶體)";
            }
            else
            {
                StatusMessage = "找不到影片資料";
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

    [RelayCommand]
    public async Task RefreshVideos()
    {
        // 清除暫存，強制重新載入
        _cachedVideos = null;
        _isFirstLoad = true;
        await LoadVideos();
    }

    private async Task LoadLocalVideosAsync(IProgress<double> progress, List<Video> tempVideos)
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
                                f.EndsWith(".mov", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                int totalFiles = files.Length;
                
                for (int i = 0; i < totalFiles; i++)
                {
                    var file = files[i];
                    var fileInfo = new System.IO.FileInfo(file);
                    var video = new Video
                    {
                        Title = System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name),
                        Description = "本機影片",
                        YoutubeUrl = fileInfo.FullName,
                        PublishDate = fileInfo.CreationTime.ToString("yyyy-MM-dd"),
                        Sys = new SystemProperties { Id = Guid.NewGuid().ToString() }
                    };

                    tempVideos.Add(video);
                    
                    // 減少延遲時間
                    if (i % 5 == 0)
                    {
                        await Task.Delay(10);
                    }

                    progress.Report((double)(i + 1) / totalFiles * 100);
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
