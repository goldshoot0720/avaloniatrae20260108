using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace avaloniatrae20260108.Views;

public partial class MusicView : UserControl
{
    public MusicView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
