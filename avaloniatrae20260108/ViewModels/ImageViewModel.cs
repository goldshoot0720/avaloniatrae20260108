using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace avaloniatrae20260108.ViewModels;

public partial class ImageViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ImageItem> _images = new();

    [ObservableProperty]
    private ImageItem? _selectedImage;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ImageViewModel()
    {
        LoadImages();
    }

    [RelayCommand]
    public async Task LoadImages()
    {
        if (IsLoading) return;

        IsLoading = true;
        Images.Clear();
        StatusMessage = "正在搜尋並載入圖片...";

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

            var loadedImages = await Task.Run(() =>
            {
                var list = new List<ImageItem>();
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
                            // Loading Bitmap here involves I/O
                            var bitmap = new Bitmap(file);
                            var imageItem = new ImageItem
                            {
                                Title = Path.GetFileName(file),
                                FilePath = file,
                                Bitmap = bitmap
                            };
                            list.Add(imageItem);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error loading image {file}: {ex.Message}");
                        }
                    }
                }
                return (list, imagesDir);
            });

            if (loadedImages.imagesDir != null)
            {
                foreach (var item in loadedImages.list)
                {
                    Images.Add(item);
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
        finally
        {
            IsLoading = false;
        }
    }
}

public class ImageItem
{
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public Bitmap? Bitmap { get; set; }
}
