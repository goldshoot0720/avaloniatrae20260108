using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Contentful.Core;
using Contentful.Core.Configuration;
using Contentful.Core.Models;
using Newtonsoft.Json;
using avaloniatrae20260108.Models;

namespace avaloniatrae20260108.ViewModels;

public partial class SubscriptionViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SubscriptionItem> _subscriptions = new();

    [ObservableProperty]
    private SubscriptionItem _newSubscription = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SubscriptionViewModel()
    {
        // Load local cache first
        LoadSubscriptions();
        // Then try to sync with Contentful
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
            // Assume content type is 'subscription'
            var entries = await client.GetEntries<SubscriptionItem>(queryString: "?content_type=subscription");

            if (entries.Any())
            {
                Subscriptions.Clear();
                foreach (var item in entries)
                {
                    if (item.NoteDocument != null)
                    {
                        item.Note = ExtractTextFromDocument(item.NoteDocument);
                    }
                    Subscriptions.Add(item);
                }
                StatusMessage = $"從 Contentful 載入 {entries.Count()} 筆訂閱";
                SaveSubscriptions(); // Update local cache
            }
            else
            {
                StatusMessage = "Contentful 上無訂閱資料";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Contentful 同步失敗: {ex.Message}";
        }
    }

    private string ExtractTextFromDocument(Document? doc)
    {
        if (doc == null) return string.Empty;
        var sb = new System.Text.StringBuilder();
        foreach (var content in doc.Content)
        {
            if (content is Paragraph p)
            {
                foreach (var child in p.Content)
                {
                    if (child is Text t)
                    {
                        sb.Append(t.Value);
                    }
                }
                sb.AppendLine();
            }
        }
        return sb.ToString().Trim();
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
    public void AddSubscription()
    {
        if (string.IsNullOrWhiteSpace(NewSubscription.Name))
        {
            StatusMessage = "請輸入訂閱名稱";
            return;
        }

        var item = new SubscriptionItem
        {
            Name = NewSubscription.Name,
            Price = NewSubscription.Price,
            NextDate = NewSubscription.NextDate,
            Site = NewSubscription.Site,
            Note = NewSubscription.Note,
            Account = NewSubscription.Account
        };

        Subscriptions.Add(item);
        StatusMessage = $"已新增: {item.Name}";
        
        // Reset inputs
        NewSubscription = new SubscriptionItem();
        SaveSubscriptions();
    }

    [RelayCommand]
    public void DeleteSubscription(SubscriptionItem item)
    {
        if (Subscriptions.Contains(item))
        {
            Subscriptions.Remove(item);
            StatusMessage = $"已刪除: {item.Name}";
            SaveSubscriptions();
        }
    }

    private void SaveSubscriptions()
    {
        try
        {
            var lines = new List<string>();
            foreach (var item in Subscriptions)
            {
                // Simple CSV-like format or just pipe separated
                // Format: Name|Price|NextDate|Site|Account|Note
                var line = $"{item.Name}|{item.Price}|{item.NextDate:yyyy-MM-dd HH:mm:ss}|{item.Site}|{item.Account}|{item.Note.Replace("\n", "\\n")}";
                lines.Add(line);
            }
            
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "subscriptions.txt");
            System.IO.File.WriteAllLines(path, lines);
            
            // Notify other views (e.g. Home)
            WeakReferenceMessenger.Default.Send(new DashboardUpdateMessage());
        }
        catch (Exception ex)
        {
            StatusMessage = $"儲存失敗: {ex.Message}";
        }
    }

    private void LoadSubscriptions()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "subscriptions.txt");
            if (System.IO.File.Exists(path))
            {
                var lines = System.IO.File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 6)
                    {
                        var item = new SubscriptionItem
                        {
                            Name = parts[0],
                            Price = int.TryParse(parts[1], out int p) ? p : 0,
                            NextDate = DateTimeOffset.TryParse(parts[2], out DateTimeOffset d) ? d : DateTimeOffset.Now,
                            Site = parts[3],
                            Account = parts[4],
                            Note = parts[5].Replace("\\n", "\n")
                        };
                        Subscriptions.Add(item);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load error: {ex.Message}");
        }
    }
}

public partial class SubscriptionItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _price;

    [ObservableProperty]
    private DateTimeOffset _nextDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string _site = string.Empty;

    [ObservableProperty]
    [property: JsonIgnore]
    private string _note = string.Empty;

    [JsonProperty("note")]
    public Document? NoteDocument { get; set; }

    [ObservableProperty]
    private string _account = string.Empty;

    [ObservableProperty]
    [property: JsonIgnore]
    private bool _isDeleting;

    [RelayCommand]
    private void RequestDelete()
    {
        IsDeleting = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleting = false;
    }
}
