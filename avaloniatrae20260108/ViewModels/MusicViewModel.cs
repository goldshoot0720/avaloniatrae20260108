using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Linq;
using LibVLCSharp.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace avaloniatrae20260108.ViewModels;

public partial class MusicViewModel : ViewModelBase, IDisposable
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;

    // 記憶體暫存
    private static List<MusicItem>? _cachedMusicList = null;
    private static bool _isFirstLoad = true;

    [ObservableProperty]
    private ObservableCollection<MusicItem> _musicList = new();

    [ObservableProperty]
    private MusicItem? _selectedMusic;

    [ObservableProperty]
    private string _currentLyrics = "請選擇一首歌曲播放";

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLyricsLoading;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private double _progressValue;
    
    [ObservableProperty]
    private double _durationMs;
    
    [ObservableProperty]
    private double _currentTimeMs;
    
    [ObservableProperty]
    private double _playbackPosition;
    
    [ObservableProperty]
    private string _currentTimeText = "00:00";
    
    [ObservableProperty]
    private string _durationText = "00:00";
    
    private bool _suppressPositionUpdate;

    public MusicViewModel()
    {
        InitializePlayer();
        // 延遲載入，避免阻塞UI
        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(100); // 讓UI先完成初始化
            await LoadMusic();
        });
    }

    private void InitializePlayer()
    {
        try
        {
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.EndReached += (s, e) => 
            {
                IsPlaying = false;
            };
            _mediaPlayer.LengthChanged += (s, e) =>
            {
                var len = _mediaPlayer?.Length ?? 0;
                Dispatcher.UIThread.Post(() =>
                {
                    DurationMs = len;
                    DurationText = FormatTime(len);
                });
            };
            _mediaPlayer.TimeChanged += (s, e) =>
            {
                var time = _mediaPlayer?.Time ?? 0;
                var len = _mediaPlayer?.Length ?? 0;
                Dispatcher.UIThread.Post(() =>
                {
                    _suppressPositionUpdate = true;
                    CurrentTimeMs = time;
                    PlaybackPosition = len > 0 ? Math.Clamp((double)time / len, 0, 1) : 0;
                    CurrentTimeText = FormatTime(time);
                    _suppressPositionUpdate = false;
                });
            };
        }
        catch (Exception ex)
        {
            StatusMessage = $"播放器初始化失敗: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task LoadMusic()
    {
        if (IsLoading) return;

        // 檢查是否有暫存資料
        if (_cachedMusicList != null && !_isFirstLoad)
        {
            IsLoading = true;
            StatusMessage = "從記憶體載入音樂...";
            
            MusicList.Clear();
            foreach (var music in _cachedMusicList)
            {
                MusicList.Add(music);
            }
            
            StatusMessage = $"已從記憶體載入 {MusicList.Count} 首歌曲";
            IsLoading = false;
            return;
        }

        // 第一次載入
        IsLoading = true;
        ProgressValue = 0;
        MusicList.Clear();
        StatusMessage = "正在搜尋並載入歌曲...";

        try
        {
            var progress = new Progress<double>(p => ProgressValue = p);
            var tempMusicList = new List<MusicItem>();

            await Task.Run(async () =>
            {
                await LoadLocalMusicAsync(progress, tempMusicList);
            });

            // 將資料加入UI並暫存
            foreach (var music in tempMusicList)
            {
                MusicList.Add(music);
            }
            
            // 暫存到記憶體
            _cachedMusicList = new List<MusicItem>(tempMusicList);
            _isFirstLoad = false;

            if (MusicList.Count > 0)
            {
                StatusMessage = $"已載入 {MusicList.Count} 首歌曲 (已暫存至記憶體)";
            }
            else
            {
                StatusMessage = "找不到 musics 資料夾";
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
    public async Task RefreshMusic()
    {
        // 清除暫存，強制重新載入
        _cachedMusicList = null;
        _isFirstLoad = true;
        await LoadMusic();
    }

    private async Task LoadLocalMusicAsync(IProgress<double> progress, List<MusicItem> tempMusicList)
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string? musicsDir = null;

            // Check potential locations
            var checkPaths = new[]
            {
                Path.Combine(baseDir, "musics"), // Release/Published
                Path.GetFullPath(Path.Combine(baseDir, "../../../musics")), 
                Path.GetFullPath(Path.Combine(baseDir, "../../../../musics")), 
                Path.GetFullPath(Path.Combine(baseDir, "../../../../../musics")) 
            };

            foreach (var path in checkPaths)
            {
                if (Directory.Exists(path))
                {
                    musicsDir = path;
                    break;
                }
            }

            if (musicsDir != null && Directory.Exists(musicsDir))
            {
                var mp3Files = Directory.GetFiles(musicsDir, "*.mp3");
                int totalFiles = mp3Files.Length;

                for (int i = 0; i < totalFiles; i++)
                {
                    var mp3 = mp3Files[i];
                    var fileName = Path.GetFileNameWithoutExtension(mp3);
                    var txtPath = Path.Combine(musicsDir, fileName + ".txt");
                    
                    var musicItem = new MusicItem
                    {
                        Title = fileName,
                        FilePath = mp3,
                        LyricsPath = File.Exists(txtPath) ? txtPath : null
                    };

                    tempMusicList.Add(musicItem);

                    // 減少延遲時間，避免阻塞
                    if (i % 5 == 0) // 每5個文件才延遲一次
                    {
                        await Task.Delay(10);
                    }

                    progress.Report((double)(i + 1) / totalFiles * 100);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Local music load error: {ex.Message}");
        }
    }

    async partial void OnSelectedMusicChanged(MusicItem? value)
    {
        if (value != null)
        {
            StatusMessage = $"準備播放: {value.Title}";
            PlayMusic(value);
            await LoadLyricsAsync(value);
            StatusMessage = $"正在播放: {value.Title}";
        }
    }

    private void PlayMusic(MusicItem item)
    {
        if (_libVLC == null || _mediaPlayer == null) return;

        try
        {
            var media = new Media(_libVLC, new Uri(item.FilePath));
            _mediaPlayer.Media = media;
            _mediaPlayer.Play();
            IsPlaying = true;
            StatusMessage = $"正在播放: {item.Title}";
            PlaybackPosition = 0;
            CurrentTimeMs = 0;
            CurrentTimeText = "00:00";
        }
        catch (Exception ex)
        {
            StatusMessage = $"播放失敗: {ex.Message}";
        }
    }

    private async Task LoadLyricsAsync(MusicItem item)
    {
        IsLyricsLoading = true;
        CurrentLyrics = string.Empty; // Clear current lyrics while loading

        try
        {
            // 適當的載入時間，讓用戶看到載入畫面但不會太久
            await Task.Delay(300);

            if (item.LyricsPath != null && File.Exists(item.LyricsPath))
            {
                var content = await File.ReadAllTextAsync(item.LyricsPath);
                
                // Format lyrics for better display
                if (!string.IsNullOrWhiteSpace(content))
                {
                    // Simple formatting: ensure proper line breaks and remove excessive whitespace
                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var formattedLines = lines.Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line));
                    CurrentLyrics = string.Join("\n\n", formattedLines);
                }
                else
                {
                    CurrentLyrics = "歌詞檔案為空";
                }
            }
            else
            {
                CurrentLyrics = "此歌曲暫無歌詞檔案\n\n♪ 請享受純音樂 ♪";
            }
        }
        catch (Exception ex)
        {
            CurrentLyrics = $"無法讀取歌詞\n\n錯誤訊息：{ex.Message}";
        }
        finally
        {
            IsLyricsLoading = false;
        }
    }

    [RelayCommand]
    public void TogglePlayPause()
    {
        if (_mediaPlayer == null) return;

        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
            IsPlaying = false;
        }
        else
        {
            _mediaPlayer.Play();
            IsPlaying = true;
        }
    }

    [RelayCommand]
    public void Stop()
    {
        if (_mediaPlayer == null) return;
        _mediaPlayer.Stop();
        IsPlaying = false;
    }

    public void Dispose()
    {
        _mediaPlayer?.Stop();
        _mediaPlayer?.Dispose();
        _libVLC?.Dispose();
    }
    
    partial void OnPlaybackPositionChanged(double value)
    {
        if (_mediaPlayer == null) return;
        if (_suppressPositionUpdate) return;
        var len = _mediaPlayer.Length;
        if (len <= 0) return;
        var target = (long)(Math.Clamp(value, 0, 1) * len);
        _mediaPlayer.Time = target;
    }
    
    private string FormatTime(long ms)
    {
        if (ms < 0) ms = 0;
        var ts = TimeSpan.FromMilliseconds(ms);
        if (ts.Hours > 0) return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}

public class MusicItem
{
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? LyricsPath { get; set; }
}
