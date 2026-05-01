using System.Collections.ObjectModel;
using System.Windows.Input;
using SM3DiagTool.Helpers;
using SM3DiagTool.J2534;
using SM3DiagTool.Logging;
using Serilog;

namespace SM3DiagTool.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly J2534Api _api;
    private readonly SessionLogger _sessionLogger;
    private ObservableObject? _currentView;
    private string _statusText = "Rozlaczony";
    private string _connectionInfo = "";
    private bool _isConnected;
    private J2534Device? _device;

    public ObservableObject? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string ConnectionInfo
    {
        get => _connectionInfo;
        set => SetProperty(ref _connectionInfo, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public ConnectionViewModel ConnectionVM { get; }
    public DtcViewModel DtcVM { get; }
    public LiveDataViewModel LiveDataVM { get; }
    public CanLoggerViewModel CanLoggerVM { get; }
    public EcuInfoViewModel EcuInfoVM { get; }
    public UdsTerminalViewModel UdsTerminalVM { get; }
    public FlashViewModel FlashVM { get; }
    public IoControlViewModel IoControlVM { get; }

    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    public ICommand ShowConnectionCommand { get; }
    public ICommand ShowDtcCommand { get; }
    public ICommand ShowLiveDataCommand { get; }
    public ICommand ShowCanLoggerCommand { get; }
    public ICommand ShowEcuInfoCommand { get; }
    public ICommand ShowUdsTerminalCommand { get; }
    public ICommand ShowFlashCommand { get; }
    public ICommand ShowIoControlCommand { get; }

    public MainViewModel()
    {
        _api = new J2534Api();
        _sessionLogger = new SessionLogger();
        _sessionLogger.OnNewEntry += entry =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                LogEntries.Add(entry);
                if (LogEntries.Count > 500)
                    LogEntries.RemoveAt(0);
            });
        };

        ConnectionVM = new ConnectionViewModel(_api, _sessionLogger, OnDeviceConnected, OnDeviceDisconnected);
        DtcVM = new DtcViewModel(_sessionLogger);
        LiveDataVM = new LiveDataViewModel(_sessionLogger);
        CanLoggerVM = new CanLoggerViewModel(_sessionLogger);
        EcuInfoVM = new EcuInfoViewModel(_sessionLogger);
        UdsTerminalVM = new UdsTerminalViewModel(_sessionLogger);
        FlashVM = new FlashViewModel(_sessionLogger);
        IoControlVM = new IoControlViewModel(_sessionLogger);

        ShowConnectionCommand = new RelayCommand(() => CurrentView = ConnectionVM);
        ShowDtcCommand = new RelayCommand(() => CurrentView = DtcVM);
        ShowLiveDataCommand = new RelayCommand(() => CurrentView = LiveDataVM);
        ShowCanLoggerCommand = new RelayCommand(() => CurrentView = CanLoggerVM);
        ShowEcuInfoCommand = new RelayCommand(() => CurrentView = EcuInfoVM);
        ShowUdsTerminalCommand = new RelayCommand(() => CurrentView = UdsTerminalVM);
        ShowFlashCommand = new RelayCommand(() => CurrentView = FlashVM);
        ShowIoControlCommand = new RelayCommand(() => CurrentView = IoControlVM);

        CurrentView = ConnectionVM;
    }

    private void OnDeviceConnected(J2534Device device)
    {
        _device = device;
        IsConnected = true;
        StatusText = $"Polaczony: {device.DeviceInfo.Name}";

        try
        {
            var voltage = device.ReadBatteryVoltage();
            ConnectionInfo = $"Napiecie: {voltage / 1000.0:F1}V";
        }
        catch
        {
            ConnectionInfo = "Polaczony";
        }

        DtcVM.SetDevice(device, _api);
        LiveDataVM.SetDevice(device, _api);
        CanLoggerVM.SetDevice(device, _api);
        EcuInfoVM.SetDevice(device, _api);
        UdsTerminalVM.SetDevice(device, _api);
        FlashVM.SetDevice(device, _api);
        IoControlVM.SetDevice(device, _api);

        _sessionLogger.LogInfo($"Polaczono z: {device.DeviceInfo.Name}");
    }

    private void OnDeviceDisconnected()
    {
        _device = null;
        IsConnected = false;
        StatusText = "Rozlaczony";
        ConnectionInfo = "";

        DtcVM.ClearDevice();
        LiveDataVM.ClearDevice();
        CanLoggerVM.ClearDevice();
        EcuInfoVM.ClearDevice();
        UdsTerminalVM.ClearDevice();
        FlashVM.ClearDevice();
        IoControlVM.ClearDevice();

        _sessionLogger.LogInfo("Rozlaczono");
    }
}
