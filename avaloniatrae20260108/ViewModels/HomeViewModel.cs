using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using avaloniatrae20260108.Models;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Threading;

namespace avaloniatrae20260108.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    public string WelcomeMessage => "歡迎使用鋒兄AI資訊系統";
    public string Description => "智能管理您的影片和圖片收藏，支援智能分類和快速搜尋";
    public string Footer => "鋒兄涂哥公開資訊© 版權所有 2025 ~ 2125";

    [ObservableProperty]
    private int _subscriptionCount;

    [ObservableProperty]
    private int _subscription7DaysCount;

    [ObservableProperty]
    private int _subscription30DaysCount;

    [ObservableProperty]
    private string _subscriptionRecentDate = "-";

    [ObservableProperty]
    private int _foodCount;

    [ObservableProperty]
    private int _food3DaysCount;

    [ObservableProperty]
    private int _food7DaysCount;

    [ObservableProperty]
    private string _foodRecentDate = "-";

    public HomeViewModel()
    {
        LoadDashboardData();

        WeakReferenceMessenger.Default.Register<DashboardUpdateMessage>(this, (r, m) =>
        {
            Dispatcher.UIThread.InvokeAsync(LoadDashboardData);
        });
    }

    private void LoadDashboardData()
    {
        LoadSubscriptionData();
        LoadFoodData();
    }

    private void LoadSubscriptionData()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "subscriptions.txt");
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                var dates = new List<DateTimeOffset>();
                
                SubscriptionCount = lines.Length;

                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 3)
                    {
                        if (DateTimeOffset.TryParse(parts[2], out DateTimeOffset nextDate))
                        {
                            dates.Add(nextDate);
                        }
                    }
                }

                var now = DateTimeOffset.Now;
                Subscription7DaysCount = dates.Count(d => d >= now && d <= now.AddDays(7));
                Subscription30DaysCount = dates.Count(d => d >= now && d <= now.AddDays(30));

                if (dates.Any())
                {
                    // Find the closest future date
                    var futureDates = dates.Where(d => d >= now).OrderBy(d => d).ToList();
                    if (futureDates.Any())
                    {
                        SubscriptionRecentDate = futureDates.First().ToString("yyyy/MM/dd");
                    }
                    else
                    {
                         // If no future dates, show the latest past date? Or just -
                         SubscriptionRecentDate = dates.Max().ToString("yyyy/MM/dd");
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    private void LoadFoodData()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "foods.txt");
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                var dates = new List<DateTimeOffset>();
                
                FoodCount = lines.Length;

                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 3)
                    {
                        if (DateTimeOffset.TryParse(parts[2], out DateTimeOffset toDate))
                        {
                            dates.Add(toDate);
                        }
                    }
                }

                var now = DateTimeOffset.Now;
                Food3DaysCount = dates.Count(d => d >= now && d <= now.AddDays(3));
                Food7DaysCount = dates.Count(d => d >= now && d <= now.AddDays(7));

                if (dates.Any())
                {
                    var futureDates = dates.Where(d => d >= now).OrderBy(d => d).ToList();
                    if (futureDates.Any())
                    {
                        FoodRecentDate = futureDates.First().ToString("yyyy/MM/dd");
                    }
                    else
                    {
                        FoodRecentDate = dates.Max().ToString("yyyy/MM/dd");
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
    }
}
