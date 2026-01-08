using Avalonia.Controls;
using avaloniatrae20260108.ViewModels;

namespace avaloniatrae20260108;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
