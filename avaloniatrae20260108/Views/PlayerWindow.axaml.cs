using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LibVLCSharp.Shared;
using LibVLCSharp.Avalonia;
using System;

namespace avaloniatrae20260108.Views;

public partial class PlayerWindow : Window
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private LibVLCSharp.Avalonia.VideoView? _videoView;

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
}
