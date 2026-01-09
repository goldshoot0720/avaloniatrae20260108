using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System;
using System.Collections.Generic;

namespace avaloniatrae20260108.ViewModels;

public partial class BankViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _welcomeMessage = "銀行速記 Bank Quick Notes";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<BankItem> _banks = new();

    [ObservableProperty]
    private decimal _totalAmount;

    public BankViewModel()
    {
        var bankNames = new[] 
        { 
            "台北富邦", "國泰世華", "兆豐銀行", "王道銀行", 
            "新光銀行", "中華郵政", "玉山銀行", "中國信託", "台新銀行" 
        };

        foreach (var name in bankNames)
        {
            var item = new BankItem { Name = name };
            item.PropertyChanged += (s, e) => 
            {
                if (e.PropertyName == nameof(BankItem.Amount))
                {
                    CalculateTotal();
                }
            };
            Banks.Add(item);
        }
    }

    private void CalculateTotal()
    {
        TotalAmount = Banks.Sum(b => b.Amount);
    }

    [RelayCommand]
    public void Save()
    {
        try
        {
            var lines = new List<string>();
            lines.Add($"銀行速記 Report - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            lines.Add(new string('-', 30));
            
            foreach (var bank in Banks)
            {
                lines.Add($"{bank.Name}: {bank.Amount:C0}");
            }
            
            lines.Add(new string('-', 30));
            lines.Add($"小計 (Subtotal): {TotalAmount:C0}");

            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bank.txt");
            File.WriteAllLines(filePath, lines);
            
            StatusMessage = $"已儲存至 {filePath} ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"儲存失敗: {ex.Message}";
        }
    }
}

public partial class BankItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private decimal _amount;
}
