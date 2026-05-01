using System.Collections.ObjectModel;
using System.Windows.Input;
using SM3DiagTool.Helpers;
using SM3DiagTool.J2534;
using SM3DiagTool.Logging;
using SM3DiagTool.Models;
using SM3DiagTool.Protocols;

namespace SM3DiagTool.ViewModels;

public class CanLoggerViewModel : ObservableObject
{
    private readonly SessionLogger _logger;
    private readonly CanLogger _canLogger = new();
    private J2534Device? _device;
    private J2534Api? _api;
    private J2534Channel? _channel;
    private CanBus? _canBus;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private string _statusText = "Polacz urzadzenie";
    private uint _baudRate = 500000;
    private bool _extendedId;
    private string _filterIdHex = "";
    private long _messageCount;
    private bool _isLoggingToFile;
    private string _selectedProtocol = "CAN 2.0";

    public ObservableCollection<CanMessage> Messages { get; } = new();
    public ObservableCollection<string> ProtocolOptions { get; } = new() { "CAN 2.0", "CAN-FD" };
    public ObservableCollection<uint> BaudRateOptions { get; } = new() { 125000, 250000, 500000, 1000000 };

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public uint BaudRate
    {
        get => _baudRate;
        set => SetProperty(ref _baudRate, value);
    }

    public bool ExtendedId
    {
        get => _extendedId;
        set => SetProperty(ref _extendedId, value);
    }

    public string FilterIdHex
    {
        get => _filterIdHex;
        set => SetProperty(ref _filterIdHex, value);
    }

    public long MessageCount
    {
        get => _messageCount;
        set => SetProperty(ref _messageCount, value);
    }

    public bool IsLoggingToFile
    {
        get => _isLoggingToFile;
        set => SetProperty(ref _isLoggingToFile, value);
    }

    public string SelectedProtocol
    {
        get => _selectedProtocol;
        set => SetProperty(ref _selectedProtocol, value);
    }

    public ICommand StartCaptureCommand { get; }
    public ICommand StopCaptureCommand { get; }
    public ICommand ClearMessagesCommand { get; }
    public ICommand StartFileLogCommand { get; }
    public ICommand StopFileLogCommand { get; }
    public ICommand SendFrameCommand { get; }
    public ICommand ExportCommand { get; }

    public string SendIdHex { get; set; } = "7DF";
    public string SendDataHex { get; set; } = "01 0C";

    public CanLoggerViewModel(SessionLogger logger)
    {
        _logger = logger;
        StartCaptureCommand = new AsyncRelayCommand(StartCaptureAsync, () => _device != null && !IsRunning);
        StopCaptureCommand = new RelayCommand(StopCapture, () => IsRunning);
        ClearMessagesCommand = new RelayCommand(() => { Messages.Clear(); MessageCount = 0; });
        StartFileLogCommand = new RelayCommand(StartFileLog, () => !IsLoggingToFile);
        StopFileLogCommand = new RelayCommand(StopFileLog, () => IsLoggingToFile);
        SendFrameCommand = new RelayCommand(SendFrame, () => IsRunning);
        ExportCommand = new RelayCommand(ExportMessages, () => Messages.Count > 0);
    }

    public void SetDevice(J2534Device device, J2534Api api)
    {
        _device = device;
        _api = api;
        StatusText = "Gotowy do przechwytywania CAN";
    }

    public void ClearDevice()
    {
        StopCapture();
        _device = null;
        _api = null;
        Messages.Clear();
        MessageCount = 0;
        StatusText = "Polacz urzadzenie";
    }

    private async Task StartCaptureAsync()
    {
        if (_device == null) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;
        StatusText = "Przechwytywanie CAN...";
        _logger.LogInfo($"Start przechwytywania CAN ({SelectedProtocol}, {BaudRate} baud)");

        try
        {
            await Task.Run(() =>
            {
                if (SelectedProtocol == "CAN-FD")
                    _channel = CanBus.OpenCanFdChannel(_device, BaudRate);
                else
                    _channel = CanBus.OpenChannel(_device, BaudRate, ExtendedId);
                _canBus = new CanBus(_channel);
            });

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var messages = await Task.Run(() => _canBus!.ReadFrames(50, 100));

                    if (messages.Count > 0)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var msg in messages)
                            {
                                Messages.Add(msg);
                                MessageCount++;

                                if (Messages.Count > 5000)
                                    Messages.RemoveAt(0);
                            }
                        });

                        if (IsLoggingToFile)
                            _canLogger.LogMessages(messages);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (J2534.J2534Exception) { }

                await Task.Delay(10, _cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusText = $"Blad przechwytywania: {ex.Message}";
            _logger.LogError("Blad przechwytywania CAN", ex);
        }
        finally
        {
            _channel?.Dispose();
            _channel = null;
            _canBus = null;
            IsRunning = false;
            StatusText = $"Zatrzymano. Przechwycono {MessageCount} wiadomosci.";
        }
    }

    private void StopCapture()
    {
        _cts?.Cancel();
        StopFileLog();
    }

    private void StartFileLog()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|ASC (*.asc)|*.asc",
            DefaultExt = ".csv",
            FileName = $"CAN_LOG_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (dialog.ShowDialog() == true)
        {
            var format = dialog.FileName.EndsWith(".asc") ? LogFormat.ASC : LogFormat.CSV;
            _canLogger.StartLogging(dialog.FileName, format);
            IsLoggingToFile = true;
            _logger.LogInfo($"Logowanie CAN do pliku: {dialog.FileName}");
        }
    }

    private void StopFileLog()
    {
        _canLogger.StopLogging();
        IsLoggingToFile = false;
    }

    private void SendFrame()
    {
        if (_canBus == null) return;
        try
        {
            var canId = Convert.ToUInt32(SendIdHex.Replace("0x", ""), 16);
            var data = SendDataHex.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Convert.ToByte(s, 16)).ToArray();
            _canBus.SendFrame(canId, data, ExtendedId);
            _logger.LogInfo($"Wyslano CAN: ID=0x{canId:X}, Data={BitConverter.ToString(data)}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Blad wysylania CAN", ex);
        }
    }

    private void ExportMessages()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            DefaultExt = ".csv",
            FileName = $"CAN_EXPORT_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (dialog.ShowDialog() == true)
        {
            using var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8);
            writer.WriteLine("Timestamp,Direction,ID,DLC,Data,ASCII");
            foreach (var msg in Messages)
                writer.WriteLine($"{msg.TimestampFormatted},{msg.Direction},{msg.IdHex},{msg.DataLength},{msg.DataHex},{msg.DataAscii}");

            _logger.LogInfo($"Wyeksportowano {Messages.Count} wiadomosci CAN");
        }
    }
}
