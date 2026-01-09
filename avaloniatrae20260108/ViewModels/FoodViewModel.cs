using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using Avalonia.Media.Imaging;
using System.IO;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using System.Net.Http;
using Contentful.Core;
using Contentful.Core.Configuration;
using Contentful.Core.Models;
using System.Linq;

namespace avaloniatrae20260108.ViewModels;

public partial class FoodViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<FoodItem> _foods = new();

    [ObservableProperty]
    private FoodItem _newFood = new();

    [ObservableProperty]
    private FoodItem? _selectedFood;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public FoodViewModel()
    {
        // Initialize with some dummy data if needed, or load from file
        LoadFoods();
        SyncContentfulCommand.Execute(null);
    }

    [RelayCommand]
    public async Task SyncContentful()
    {
        StatusMessage = "正在從 Contentful 同步...";
        try
        {
            var settings = LoadSettings();
            if (string.IsNullOrEmpty(settings.SpaceId) || string.IsNullOrEmpty(settings.AccessToken))
            {
                StatusMessage = "Contentful 設定未完成，僅顯示本機資料";
                return;
            }

            var options = new ContentfulOptions
            {
                DeliveryApiKey = settings.AccessToken,
                SpaceId = settings.SpaceId
            };

            var client = new ContentfulClient(new HttpClient(), options);
            var entries = await client.GetEntries<ContentfulFood>(queryString: "?content_type=food");

            if (entries.Any())
            {
                Foods.Clear();
                foreach (var item in entries)
                {
                    var foodItem = new FoodItem
                    {
                        Name = item.Name,
                        Amount = item.Amount,
                        ToDate = item.ToDate,
                        Price = item.Price,
                        Shop = item.Shop,
                        PhotoHash = item.PhotoHash
                    };

                    if (item.Photo?.File?.Url != null)
                    {
                        try
                        {
                            var url = "https:" + item.Photo.File.Url;
                            foodItem.PhotoPath = url;
                            
                            using var http = new HttpClient();
                            var bytes = await http.GetByteArrayAsync(url);
                            using var stream = new MemoryStream(bytes);
                            foodItem.PhotoBitmap = new Bitmap(stream);
                        }
                        catch
                        {
                            // Ignore image load error
                        }
                    }

                    Foods.Add(foodItem);
                }
                StatusMessage = $"從 Contentful 載入 {entries.Count()} 筆食品";
                SaveFoods();
            }
            else
            {
                StatusMessage = "Contentful 上無食品資料";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Contentful 同步失敗: {ex.Message}";
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

    [RelayCommand]
    public void AddFood()
    {
        if (string.IsNullOrWhiteSpace(NewFood.Name))
        {
            StatusMessage = "請輸入食品名稱";
            return;
        }

        var item = new FoodItem
        {
            Name = NewFood.Name,
            Amount = NewFood.Amount,
            ToDate = NewFood.ToDate,
            Price = NewFood.Price,
            Shop = NewFood.Shop,
            PhotoPath = NewFood.PhotoPath,
            PhotoBitmap = NewFood.PhotoBitmap,
            PhotoHash = NewFood.PhotoHash
        };

        Foods.Add(item);
        StatusMessage = $"已新增: {item.Name}";
        SaveFoods();
        
        // Reset NewFood but keep the date today or logic as needed
        NewFood = new FoodItem(); 
    }

    [RelayCommand]
    public void DeleteFood(FoodItem item)
    {
        if (Foods.Contains(item))
        {
            Foods.Remove(item);
            StatusMessage = $"已刪除: {item.Name}";
            SaveFoods();
        }
    }
    
    // Logic to handle file picking will be invoked from View usually, 
    // or we can pass a storage provider. 
    // For MVVM purity, we might need a service, but for this simpler app, 
    // we can expose a method or command that receives the file path.

    public void SetPhoto(string path)
    {
        try 
        {
            if (System.IO.File.Exists(path))
            {
                NewFood.PhotoPath = path;
                NewFood.PhotoBitmap = new Bitmap(path);
                // Simple hash simulation or actual hash if needed
                NewFood.PhotoHash = Guid.NewGuid().ToString("N").Substring(0, 8); 
                OnPropertyChanged(nameof(NewFood));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"圖片載入失敗: {ex.Message}";
        }
    }

    private void SaveFoods()
    {
        try
        {
            var lines = new System.Collections.Generic.List<string>();
            foreach (var item in Foods)
            {
                // Format: Name|Amount|ToDate|Price|Shop|PhotoPath|PhotoHash
                var line = $"{item.Name}|{item.Amount}|{item.ToDate:yyyy-MM-dd HH:mm:ss}|{item.Price}|{item.Shop}|{item.PhotoPath}|{item.PhotoHash}";
                lines.Add(line);
            }
            
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "foods.txt");
            System.IO.File.WriteAllLines(path, lines);
        }
        catch (Exception ex)
        {
            StatusMessage = $"儲存失敗: {ex.Message}";
        }
    }

    private void LoadFoods()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "foods.txt");
            if (System.IO.File.Exists(path))
            {
                var lines = System.IO.File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 7)
                    {
                        var item = new FoodItem
                        {
                            Name = parts[0],
                            Amount = int.TryParse(parts[1], out int a) ? a : 1,
                            ToDate = DateTimeOffset.TryParse(parts[2], out DateTimeOffset d) ? d : DateTimeOffset.Now,
                            Price = int.TryParse(parts[3], out int p) ? p : 0,
                            Shop = parts[4],
                            PhotoPath = parts[5],
                            PhotoHash = parts[6]
                        };

                        if (!string.IsNullOrEmpty(item.PhotoPath) && System.IO.File.Exists(item.PhotoPath))
                        {
                            try
                            {
                                item.PhotoBitmap = new Bitmap(item.PhotoPath);
                            }
                            catch { /* Ignore image load error */ }
                        }
                        else if (!string.IsNullOrEmpty(item.PhotoPath) && item.PhotoPath.StartsWith("http"))
                        {
                             // Async load for http images not handled here to avoid complexity in sync Load, 
                             // but normally we would trigger a download. 
                             // For now, we skip bitmap if not local.
                        }

                        Foods.Add(item);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load foods error: {ex.Message}");
        }
    }
}

public partial class FoodItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _amount = 1;

    [ObservableProperty]
    private DateTimeOffset _toDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string _photoPath = string.Empty;

    [ObservableProperty]
    private Bitmap? _photoBitmap;

    [ObservableProperty]
    private int _price;

    [ObservableProperty]
    private string _shop = string.Empty;

    [ObservableProperty]
    private string _photoHash = string.Empty;
}

public class ContentfulFood
{
    public string Name { get; set; }
    public int Amount { get; set; }
    public DateTime ToDate { get; set; }
    public Asset? Photo { get; set; }
    public int Price { get; set; }
    public string Shop { get; set; }
    public string PhotoHash { get; set; }
}
