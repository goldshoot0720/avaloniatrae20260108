using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    private CancellationTokenSource? _saveCts;
    private bool _isLoading;

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
                    TriggerAutoSave();
                }
            };
            Banks.Add(item);
        }

        // Auto load on startup
        Load();
    }

    private void TriggerAutoSave()
    {
        if (_isLoading) return;

        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(5000, token);
                if (!token.IsCancellationRequested)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Invoke(() => Save());
                }
            }
            catch (TaskCanceledException)
            {
                // Ignored
            }
        });
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

    [RelayCommand]
    public void Load()
    {
        _isLoading = true;
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bank.txt");
            if (!File.Exists(filePath))
            {
                StatusMessage = "找不到存檔 (File not found)";
                return;
            }

            var lines = File.ReadAllLines(filePath);
            int loadedCount = 0;

            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length >= 2)
                {
                    var name = parts[0].Trim();
                    var amountStr = string.Join(":", parts.Skip(1)).Trim(); 

                    // Remove non-numeric chars except . and - (to handle currency symbols and commas)
                    var cleanAmountStr = new string(amountStr.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());

                    if (decimal.TryParse(cleanAmountStr, out decimal amount))
                    {
                        var bank = Banks.FirstOrDefault(b => b.Name == name);
                        if (bank != null)
                        {
                            bank.Amount = amount;
                            loadedCount++;
                        }
                    }
                }
            }
            
            StatusMessage = $"已讀取 {loadedCount} 筆資料 ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"讀取失敗: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
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
