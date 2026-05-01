using System.Collections.ObjectModel;
using System.Windows.Input;
using SM3DiagTool.Helpers;
using SM3DiagTool.J2534;
using SM3DiagTool.Logging;
using SM3DiagTool.Protocols;

namespace SM3DiagTool.ViewModels;

public class UdsTerminalViewModel : ObservableObject
{
    private readonly SessionLogger _logger;
    private J2534Device? _device;
    private J2534Api? _api;
    private J2534Channel? _channel;
    private Uds? _uds;
    private string _statusText = "Polacz urzadzenie";
    private string _txIdHex = "7E0";
    private string _rxIdHex = "7E8";
    private string _requestHex = "22 F1 90";
    private bool _isConnected;
    private uint _baudRate = 500000;
    private string _selectedProtocol = "ISO 15765 (CAN)";

    public ObservableCollection<TerminalEntry> History { get; } = new();
    public ObservableCollection<string> ProtocolOptions { get; } = new()
    {
        "ISO 15765 (CAN)", "ISO 14230 (K-LINE)", "CAN Raw"
    };
    public ObservableCollection<string> PresetCommands { get; } = new()
    {
        "10 01 - Default Session",
        "10 02 - Programming Session",
        "10 03 - Extended Session",
        "22 F1 90 - Read VIN",
        "22 F1 97 - Read System Name",
        "22 F1 87 - Read Part Number",
        "22 F1 88 - Read SW Number",
        "22 F1 91 - Read HW Number",
        "19 02 FF - Read DTCs",
        "14 FF FF FF - Clear DTCs",
        "3E 00 - Tester Present",
        "27 01 - Security Access (Request Seed)",
    };

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string TxIdHex
    {
        get => _txIdHex;
        set => SetProperty(ref _txIdHex, value);
    }

    public string RxIdHex
    {
        get => _rxIdHex;
        set => SetProperty(ref _rxIdHex, value);
    }

    public string RequestHex
    {
        get => _requestHex;
        set => SetProperty(ref _requestHex, value);
    }

    public bool IsChannelOpen
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public uint BaudRate
    {
        get => _baudRate;
        set => SetProperty(ref _baudRate, value);
    }

    public string SelectedProtocol
    {
        get => _selectedProtocol;
        set => SetProperty(ref _selectedProtocol, value);
    }

    public ICommand OpenChannelCommand { get; }
    public ICommand CloseChannelCommand { get; }
    public ICommand SendRequestCommand { get; }
    public ICommand UsePresetCommand { get; }
    public ICommand ClearHistoryCommand { get; }

    public UdsTerminalViewModel(SessionLogger logger)
    {
        _logger = logger;
        OpenChannelCommand = new AsyncRelayCommand(OpenChannelAsync, () => _device != null && !IsChannelOpen);
        CloseChannelCommand = new RelayCommand(CloseChannel, () => IsChannelOpen);
        SendRequestCommand = new AsyncRelayCommand(SendRequestAsync, () => IsChannelOpen);
        UsePresetCommand = new RelayCommand(p =>
        {
            if (p is string preset)
                RequestHex = preset.Split(" - ")[0];
        });
        ClearHistoryCommand = new RelayCommand(() => History.Clear());
    }

    public void SetDevice(J2534Device device, J2534Api api)
    {
        _device = device;
        _api = api;
        StatusText = "Gotowy - otworz kanal komunikacji";
    }

    public void ClearDevice()
    {
        CloseChannel();
        _device = null;
        _api = null;
        StatusText = "Polacz urzadzenie";
    }

    private async Task OpenChannelAsync()
    {
        if (_device == null) return;

        StatusText = "Otwieranie kanalu...";

        try
        {
            var txId = Convert.ToUInt32(TxIdHex.Replace("0x", ""), 16);
            var rxId = Convert.ToUInt32(RxIdHex.Replace("0x", ""), 16);

            await Task.Run(() =>
            {
                _channel = Uds.OpenChannel(_device, txId, rxId, BaudRate);
                _uds = new Uds(_channel, txId, rxId);
            });

            IsChannelOpen = true;
            StatusText = $"Kanal otwarty: TX=0x{txId:X3}, RX=0x{rxId:X3}";
            AddHistoryEntry("SYSTEM", $"Kanal otwarty ({SelectedProtocol}, {BaudRate} baud)");
            _logger.LogInfo($"UDS channel opened: TX=0x{txId:X3}, RX=0x{rxId:X3}");
        }
        catch (Exception ex)
        {
            StatusText = $"Blad otwierania kanalu: {ex.Message}";
            _logger.LogError("Blad otwierania kanalu UDS", ex);
        }
    }

    private void CloseChannel()
    {
        _channel?.Dispose();
        _channel = null;
        _uds = null;
        IsChannelOpen = false;
        StatusText = "Kanal zamkniety";
    }

    private async Task SendRequestAsync()
    {
        if (_uds == null || _channel == null) return;

        try
        {
            var bytes = RequestHex.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Convert.ToByte(s, 16)).ToArray();

            if (bytes.Length == 0) return;

            AddHistoryEntry("TX", RequestHex.ToUpper());
            _logger.LogData("TX", bytes, "UDS");

            var response = await Task.Run(() =>
            {
                var serviceId = bytes[0];
                var subData = bytes.Length > 1 ? bytes.Skip(1).ToArray() : null;
                return _uds.SendRequest(serviceId, subData);
            });

            if (response.Length > 0)
            {
                var responseHex = string.Join(" ", response.Select(b => $"{b:X2}"));
                AddHistoryEntry("RX", responseHex);
                _logger.LogData("RX", response, "UDS");

                var interpretation = InterpretResponse(response);
                if (!string.IsNullOrEmpty(interpretation))
                    AddHistoryEntry("INFO", interpretation);
            }
            else
            {
                AddHistoryEntry("RX", "Brak odpowiedzi");
            }
        }
        catch (J2534.J2534Exception ex)
        {
            AddHistoryEntry("ERROR", ex.Message);
            _logger.LogError("UDS request error", ex);
        }
        catch (Exception ex)
        {
            AddHistoryEntry("ERROR", ex.Message);
            _logger.LogError("UDS request error", ex);
        }
    }

    private void AddHistoryEntry(string direction, string data)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            History.Add(new TerminalEntry
            {
                Timestamp = DateTime.Now,
                Direction = direction,
                Data = data
            });

            if (History.Count > 1000)
                History.RemoveAt(0);
        });
    }

    private static string InterpretResponse(byte[] response)
    {
        if (response.Length < 1) return "";

        byte positiveResponse = response[0];
        return positiveResponse switch
        {
            0x50 => $"Session Control: sesja 0x{(response.Length > 1 ? response[1] : 0):X2}",
            0x62 when response.Length > 2 =>
                $"Read DID 0x{response[1]:X2}{response[2]:X2}: " +
                new string(response.Skip(3).Select(b => b >= 0x20 && b <= 0x7E ? (char)b : '.').ToArray()),
            0x59 => $"DTC Info: {(response.Length - 1) / 4} kodow bledow",
            0x54 => "DTC skasowane pomyslnie",
            0x67 => response.Length > 1 ? $"Security Access: level 0x{response[1]:X2}" : "Security Access",
            0x7E => "Tester Present OK",
            _ => ""
        };
    }
}

public class TerminalEntry
{
    public DateTime Timestamp { get; set; }
    public string Direction { get; set; } = "";
    public string Data { get; set; } = "";
    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss.fff");
}
