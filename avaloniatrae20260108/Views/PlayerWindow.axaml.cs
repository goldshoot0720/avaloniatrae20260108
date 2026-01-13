using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LibVLCSharp.Shared;
using LibVLCSharp.Avalonia;
using System;
using Avalonia.Threading;

namespace avaloniatrae20260108.Views;

public partial class PlayerWindow : Window
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private LibVLCSharp.Avalonia.VideoView? _videoView;
    private Slider? _timeline;
    private TextBlock? _lblTime;
    private TextBlock? _lblDuration;
    private bool _suppressPositionUpdate;

    public PlayerWindow()
    {
        InitializeComponent();
    }

    public PlayerWindow(string url) : this()
    {
        // Initialize LibVLC
        try
        {
            Core.Initialize();
        }
        catch (Exception ex)
        {
             // Handle initialization error (e.g. missing libvlc)
             System.Diagnostics.Debug.WriteLine($"LibVLC Init Error: {ex.Message}");
        }

        _libVLC = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVLC);

        // Create VideoView dynamically
        _videoView = new LibVLCSharp.Avalonia.VideoView();
        _videoView.MediaPlayer = _mediaPlayer;

        var container = this.FindControl<Border>("VideoContainer");
        if (container != null)
        {
            container.Child = _videoView;
        }

        // Setup Controls
        var btnStop = this.FindControl<Button>("BtnStop");
        var btnPlay = this.FindControl<Button>("BtnPlay");
        var btnPause = this.FindControl<Button>("BtnPause");

        if (btnStop != null) btnStop.Click += (s, e) => _mediaPlayer.Stop();
        if (btnPlay != null) btnPlay.Click += (s, e) => _mediaPlayer.Play();
        if (btnPause != null) btnPause.Click += (s, e) => _mediaPlayer.Pause();
        
        _timeline = this.FindControl<Slider>("Timeline");
        _lblTime = this.FindControl<TextBlock>("LblTime");
        _lblDuration = this.FindControl<TextBlock>("LblDuration");
        
        if (_timeline != null)
        {
            _timeline.PropertyChanged += (s, e) =>
            {
                if (e.Property != Slider.ValueProperty) return;
                if (_mediaPlayer == null) return;
                if (_suppressPositionUpdate) return;
                var pos = Math.Clamp(_timeline.Value / 100.0, 0, 1);
                _mediaPlayer.Position = (float)pos;
            };
        }
        
        if (_mediaPlayer != null)
        {
            _mediaPlayer.LengthChanged += (s, e) =>
            {
                var len = _mediaPlayer?.Length ?? 0;
                var text = FormatTime(len);
                Dispatcher.UIThread.Post(() =>
                {
                    if (_lblDuration != null) _lblDuration.Text = text;
                });
            };
            
            _mediaPlayer.TimeChanged += (s, e) =>
            {
                var time = _mediaPlayer?.Time ?? 0;
                var len = _mediaPlayer?.Length ?? 0;
                var pos = len > 0 ? Math.Clamp((double)time / len, 0, 1) : 0;
                var timeText = FormatTime(time);
                Dispatcher.UIThread.Post(() =>
                {
                    _suppressPositionUpdate = true;
                    if (_timeline != null) _timeline.Value = pos * 100.0;
                    if (_lblTime != null) _lblTime.Text = timeText;
                    _suppressPositionUpdate = false;
                });
            };
        }

        this.Closing += (s, e) => 
        {
            // Detach safely
            if (_videoView != null) _videoView.MediaPlayer = null;
            
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            
            _mediaPlayer = null;
            _libVLC = null;
        };

        PlayVideo(url);
    }

    private void PlayVideo(string url)
    {
        if (string.IsNullOrEmpty(url) || _libVLC == null || _mediaPlayer == null) return;

        try 
        {
            var media = new Media(_libVLC, new Uri(url));
            _mediaPlayer.Play(media);
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Play Error: {ex.Message}");
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private string FormatTime(long ms)
    {
        if (ms < 0) ms = 0;
        var ts = TimeSpan.FromMilliseconds(ms);
        if (ts.Hours > 0) return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
