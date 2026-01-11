using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Linq;
using LibVLCSharp.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace avaloniatrae20260108.ViewModels;

public partial class MusicViewModel : ViewModelBase, IDisposable
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;

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

    public MusicViewModel()
    {
        InitializePlayer();
        LoadMusic();
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"播放器初始化失敗: {ex.Message}";
        }
    }

    [RelayCommand]
    public void LoadMusic()
    {
        MusicList.Clear();
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
                foreach (var mp3 in mp3Files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(mp3);
                    var txtPath = Path.Combine(musicsDir, fileName + ".txt");
                    
                    var musicItem = new MusicItem
                    {
                        Title = fileName,
                        FilePath = mp3,
                        LyricsPath = File.Exists(txtPath) ? txtPath : null
                    };
                    MusicList.Add(musicItem);
                }
                StatusMessage = $"已載入 {MusicList.Count} 首歌曲";
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
    }

    async partial void OnSelectedMusicChanged(MusicItem? value)
    {
        if (value != null)
        {
            PlayMusic(value);
            await LoadLyricsAsync(value);
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
            // Simulate loading delay to show the indicator (as requested by user to have a visible hint)
            await Task.Delay(500);

            if (item.LyricsPath != null && File.Exists(item.LyricsPath))
            {
                var content = await File.ReadAllTextAsync(item.LyricsPath);
                // Simple LRC parsing logic could be added here to format it nicely
                // For now, just show raw or lightly formatted text
                CurrentLyrics = content;
            }
            else
            {
                CurrentLyrics = "無歌詞檔案";
            }
        }
        catch (Exception ex)
        {
            CurrentLyrics = $"無法讀取歌詞: {ex.Message}";
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
}

public class MusicItem
{
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? LyricsPath { get; set; }
}
