using System.Collections.ObjectModel;
using System.Windows.Input;
using SM3DiagTool.Helpers;
using SM3DiagTool.J2534;
using SM3DiagTool.Logging;
using SM3DiagTool.Protocols;

namespace SM3DiagTool.ViewModels;

public class FlashViewModel : ObservableObject
{
    private readonly SessionLogger _logger;
    private J2534Device? _device;
    private J2534Api? _api;
    private string _statusText = "Polacz urzadzenie";
    private bool _isRunning;
    private int _progress;
    private int _progressMax = 100;
    private string _memoryAddressHex = "00000000";
    private string _memorySizeHex = "00001000";
    private string _hexDump = "";

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsRunning { get => _isRunning; set => SetProperty(ref _isRunning, value); }
    public int Progress { get => _progress; set => SetProperty(ref _progress, value); }
    public int ProgressMax { get => _progressMax; set => SetProperty(ref _progressMax, value); }
    public string MemoryAddressHex { get => _memoryAddressHex; set => SetProperty(ref _memoryAddressHex, value); }
    public string MemorySizeHex { get => _memorySizeHex; set => SetProperty(ref _memorySizeHex, value); }
    public string HexDump { get => _hexDump; set => SetProperty(ref _hexDump, value); }

    public ObservableCollection<string> Log { get; } = new();

    public ICommand ReadMemoryCommand { get; }
    public ICommand WriteMemoryCommand { get; }
    public ICommand SaveToFileCommand { get; }
    public ICommand LoadFromFileCommand { get; }
    public ICommand EraseMemoryCommand { get; }

    public FlashViewModel(SessionLogger logger)
    {
        _logger = logger;
        ReadMemoryCommand = new AsyncRelayCommand(ReadMemoryAsync, () => _device != null && !IsRunning);
        WriteMemoryCommand = new AsyncRelayCommand(WriteMemoryAsync, () => _device != null && !IsRunning);
        SaveToFileCommand = new RelayCommand(SaveToFile, () => !string.IsNullOrEmpty(HexDump));
        LoadFromFileCommand = new RelayCommand(LoadFromFile, () => !IsRunning);
        EraseMemoryCommand = new AsyncRelayCommand(EraseMemoryAsync, () => _device != null && !IsRunning);
    }

    public void SetDevice(J2534Device device, J2534Api api)
    {
        _device = device;
        _api = api;
        StatusText = "Gotowy do operacji flash";
    }

    public void ClearDevice()
    {
        _device = null;
        _api = null;
        StatusText = "Polacz urzadzenie";
    }

    private async Task ReadMemoryAsync()
    {
        if (_device == null) return;
        IsRunning = true;
        Progress = 0;
        HexDump = "";
        AddLog("Rozpoczynam odczyt pamieci...");

        try
        {
            var address = Convert.ToUInt32(MemoryAddressHex, 16);
            var size = Convert.ToUInt32(MemorySizeHex, 16);

            var data = await Task.Run(() =>
            {
                using var channel = Uds.OpenChannel(_device, 0x7E0, 0x7E8);
                var uds = new Uds(channel);
                uds.StartSession(Uds.SessionType.Programming);

                var security = new SecurityManager(uds);
                security.UnlockLevel(0x01);

                var flash = new FlashManager(uds);
                flash.ProgressChanged += (current, total) =>
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        Progress = current;
                        ProgressMax = total;
                    });
                flash.StatusChanged += status =>
                    System.Windows.Application.Current?.Dispatcher.Invoke(() => AddLog(status));

                return flash.ReadMemory(address, size);
            });

            if (data.Length > 0)
            {
                HexDump = FormatHexDump(data, Convert.ToUInt32(MemoryAddressHex, 16));
                StatusText = $"Odczytano {data.Length} bajtow";
                AddLog($"Odczyt zakonczony: {data.Length} bajtow");
            }
            else
            {
                StatusText = "Odczyt nie powiodl sie";
                AddLog("Blad odczytu pamieci");
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Blad: {ex.Message}";
            AddLog($"Blad: {ex.Message}");
            _logger.LogError("Flash read error", ex);
        }
        finally { IsRunning = false; }
    }

    private async Task WriteMemoryAsync()
    {
        if (_device == null) return;
        IsRunning = true;
        Progress = 0;
        AddLog("Rozpoczynam zapis pamieci...");

        try
        {
            var address = Convert.ToUInt32(MemoryAddressHex, 16);
            var data = ParseHexDump(HexDump);

            var success = await Task.Run(() =>
            {
                using var channel = Uds.OpenChannel(_device, 0x7E0, 0x7E8);
                var uds = new Uds(channel);
                uds.StartSession(Uds.SessionType.Programming);

                var security = new SecurityManager(uds);
                security.UnlockLevel(0x01);

                var flash = new FlashManager(uds);
                flash.ProgressChanged += (current, total) =>
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        Progress = current;
                        ProgressMax = total;
                    });
                flash.StatusChanged += status =>
                    System.Windows.Application.Current?.Dispatcher.Invoke(() => AddLog(status));

                return flash.WriteMemory(address, data);
            });

            StatusText = success ? "Zapis zakonczony pomyslnie" : "Zapis nie powiodl sie";
            AddLog(success ? "Zapis zakonczony" : "Blad zapisu");
        }
        catch (Exception ex)
        {
            StatusText = $"Blad: {ex.Message}";
            AddLog($"Blad: {ex.Message}");
            _logger.LogError("Flash write error", ex);
        }
        finally { IsRunning = false; }
    }

    private async Task EraseMemoryAsync()
    {
        if (_device == null) return;
        IsRunning = true;
        AddLog("Kasowanie pamieci...");

        try
        {
            var address = Convert.ToUInt32(MemoryAddressHex, 16);
            var size = Convert.ToUInt32(MemorySizeHex, 16);

            var success = await Task.Run(() =>
            {
                using var channel = Uds.OpenChannel(_device, 0x7E0, 0x7E8);
                var uds = new Uds(channel);
                uds.StartSession(Uds.SessionType.Programming);

                var security = new SecurityManager(uds);
                security.UnlockLevel(0x01);

                var flash = new FlashManager(uds);
                return flash.EraseMemory(address, size);
            });

            StatusText = success ? "Pamiec skasowana" : "Kasowanie nie powiodlo sie";
        }
        catch (Exception ex)
        {
            StatusText = $"Blad: {ex.Message}";
            _logger.LogError("Flash erase error", ex);
        }
        finally { IsRunning = false; }
    }

    private void SaveToFile()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "BIN (*.bin)|*.bin|HEX (*.hex)|*.hex",
            DefaultExt = ".bin",
            FileName = $"FLASH_{MemoryAddressHex}_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (dialog.ShowDialog() == true)
        {
            var data = ParseHexDump(HexDump);
            System.IO.File.WriteAllBytes(dialog.FileName, data);
            AddLog($"Zapisano do: {dialog.FileName}");
        }
    }

    private void LoadFromFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "BIN (*.bin)|*.bin|HEX (*.hex)|*.hex|Wszystkie (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            var data = System.IO.File.ReadAllBytes(dialog.FileName);
            HexDump = FormatHexDump(data, Convert.ToUInt32(MemoryAddressHex, 16));
            AddLog($"Wczytano z pliku: {dialog.FileName} ({data.Length} bajtow)");
        }
    }

    private void AddLog(string message)
    {
        Log.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        if (Log.Count > 200) Log.RemoveAt(0);
    }

    private static string FormatHexDump(byte[] data, uint baseAddress)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < data.Length; i += 16)
        {
            sb.Append($"{baseAddress + i:X8}: ");
            for (int j = 0; j < 16; j++)
            {
                if (i + j < data.Length)
                    sb.Append($"{data[i + j]:X2} ");
                else
                    sb.Append("   ");
            }
            sb.Append(" ");
            for (int j = 0; j < 16 && i + j < data.Length; j++)
            {
                byte b = data[i + j];
                sb.Append(b >= 0x20 && b <= 0x7E ? (char)b : '.');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static byte[] ParseHexDump(string hexDump)
    {
        var bytes = new List<byte>();
        foreach (var line in hexDump.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0) continue;
            var hex = line.Substring(colonIdx + 1).Trim();
            var spaceIdx = hex.IndexOf("  ");
            if (spaceIdx > 0) hex = hex.Substring(0, spaceIdx);

            foreach (var byteStr in hex.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (byte.TryParse(byteStr, System.Globalization.NumberStyles.HexNumber, null, out byte b))
                    bytes.Add(b);
            }
        }
        return bytes.ToArray();
    }
}
