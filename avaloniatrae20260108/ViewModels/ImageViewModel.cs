using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace avaloniatrae20260108.ViewModels;

public partial class ImageViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ImageItem> _images = new();

    [ObservableProperty]
    private ImageItem? _selectedImage;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ImageViewModel()
    {
        LoadImages();
    }

    [RelayCommand]
    public void LoadImages()
    {
        Images.Clear();
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string? imagesDir = null;

            // Check potential locations
            var checkPaths = new[]
            {
                Path.Combine(baseDir, "images"), // Release/Published
                Path.GetFullPath(Path.Combine(baseDir, "../../../images")), 
                Path.GetFullPath(Path.Combine(baseDir, "../../../../images")), 
                Path.GetFullPath(Path.Combine(baseDir, "../../../../../images")) 
            };

            foreach (var path in checkPaths)
            {
                if (Directory.Exists(path))
                {
                    imagesDir = path;
                    break;
                }
            }

            if (imagesDir != null && Directory.Exists(imagesDir))
            {
                var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
                var files = Directory.GetFiles(imagesDir, "*.*")
                                     .Where(s => supportedExtensions.Contains(Path.GetExtension(s).ToLower()));

                foreach (var file in files)
                {
                    try
                    {
                        var imageItem = new ImageItem
                        {
                            Title = Path.GetFileName(file),
                            FilePath = file,
                            // Load bitmap for display (be careful with large images, maybe load async or thumbnail in real app)
                            // For simplicity here, we just keep path and let View bind to it or load it
                            // Avalonia Image control can bind to string path directly with a converter, or we can create Bitmap
                            Bitmap = new Bitmap(file) 
                        };
                        Images.Add(imageItem);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading image {file}: {ex.Message}");
                    }
                }
                StatusMessage = $"已載入 {Images.Count} 張圖片";
            }
            else
            {
                StatusMessage = "找不到 images 資料夾";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"載入失敗: {ex.Message}";
        }
    }
}

public class ImageItem
{
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public Bitmap? Bitmap { get; set; }
}
