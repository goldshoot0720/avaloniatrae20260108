using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using System;

namespace avaloniatrae20260108.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private object _currentView = new HomeViewModel(); // Initialize to avoid warning

    [ObservableProperty]
    private ListItemTemplate _selectedListItem;
    
    [ObservableProperty]
    private bool _isPaneOpen = true;

    private ImageViewModel? _imageViewModel;
    private FoodViewModel? _foodViewModel;
    private SubscriptionViewModel? _subscriptionViewModel;

    partial void OnSelectedListItemChanged(ListItemTemplate value)
    {
        if (value != null)
        {
             SwitchView(value.ModelType);
        }
    }

    public ObservableCollection<ListItemTemplate> Items { get; } = new();

    public MainViewModel()
    {
        // Initialize Navigation Items
        Items.Add(new ListItemTemplate(typeof(HomeViewModel), "Home", "首頁"));
        Items.Add(new ListItemTemplate(typeof(ImageViewModel), "Image", "圖片庫"));
        Items.Add(new ListItemTemplate(typeof(VideoViewModel), "Video", "影片庫"));
        Items.Add(new ListItemTemplate(typeof(MusicViewModel), "Music", "鋒兄音樂歌詞"));
        Items.Add(new ListItemTemplate(typeof(BankViewModel), "Bank", "銀行速記"));
        Items.Add(new ListItemTemplate(typeof(SubscriptionViewModel), "Subscription", "訂閱管理"));
        Items.Add(new ListItemTemplate(typeof(FoodViewModel), "Food", "食品管理"));
        Items.Add(new ListItemTemplate(typeof(SettingsViewModel), "Settings", "系統設定"));

        // Default view
        SelectedListItem = Items[0];
    }

    [RelayCommand]
    public void TriggerPane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    public void SwitchView(Type type)
    {
        if (CurrentView is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (type == typeof(HomeViewModel))
        {
            CurrentView = new HomeViewModel();
        }
        else if (type == typeof(SettingsViewModel))
        {
            CurrentView = new SettingsViewModel();
        }
        else if (type == typeof(ImageViewModel))
        {
            if (_imageViewModel == null)
            {
                _imageViewModel = new ImageViewModel();
            }
            CurrentView = _imageViewModel;
        }
        else if (type == typeof(VideoViewModel))
        {
            CurrentView = new VideoViewModel();
        }
        else if (type == typeof(MusicViewModel))
        {
            CurrentView = new MusicViewModel();
        }
        else if (type == typeof(BankViewModel))
        {
            CurrentView = new BankViewModel();
        }
        else if (type == typeof(FoodViewModel))
        {
            if (_foodViewModel == null)
            {
                _foodViewModel = new FoodViewModel();
            }
            CurrentView = _foodViewModel;
        }
        else if (type == typeof(SubscriptionViewModel))
        {
            if (_subscriptionViewModel == null)
            {
                _subscriptionViewModel = new SubscriptionViewModel();
            }
            CurrentView = _subscriptionViewModel;
        }
        else
        {
             // Placeholder for others
             // CurrentView = Activator.CreateInstance(type);
        }
    }
}

public class ListItemTemplate
{
    public string Label { get; }
    public Type ModelType { get; }
    public string IconKey { get; }

    public ListItemTemplate(Type type, string iconKey, string label)
    {
        ModelType = type;
        Label = label;
        IconKey = iconKey;
    }
}
