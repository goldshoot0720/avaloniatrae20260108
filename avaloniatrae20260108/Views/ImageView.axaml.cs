using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace avaloniatrae20260108.Views;

public partial class ImageView : UserControl
{
    public ImageView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
