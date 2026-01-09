using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using avaloniatrae20260108.ViewModels;
using System.Linq;

namespace avaloniatrae20260108.Views;

public partial class FoodView : UserControl
{
    public FoodView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "選擇食品圖片",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (files.Count >= 1)
        {
            var filePath = files[0].Path.LocalPath;
            if (DataContext is FoodViewModel vm)
            {
                vm.SetPhoto(filePath);
            }
        }
    }
}
