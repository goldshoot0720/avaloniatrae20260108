using CommunityToolkit.Mvvm.ComponentModel;

namespace avaloniatrae20260108.ViewModels;

public partial class BankViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _welcomeMessage = "銀行速記 Bank Quick Notes";
}
