using System.Collections.ObjectModel;
using System.Windows.Input;
using SM3DiagTool.Helpers;
using SM3DiagTool.J2534;
using SM3DiagTool.Logging;

namespace SM3DiagTool.ViewModels;

public class ConnectionViewModel : ObservableObject
{
    private readonly J2534Api _api;
    private readonly SessionLogger _logger;
    private readonly Action<J2534Device> _onConnected;
    private readonly Action _onDisconnected;
    private J2534Device? _device;
    private J2534DeviceInfo? _selectedDevice;
    private string _firmwareVersion = "";
    private string _dllVersion = "";
    private string _apiVersion = "";
    private string _batteryVoltage = "";
    private bool _isConnected;
    private string _connectionStatus = "Rozlaczony";

    public ObservableCollection<J2534DeviceInfo> AvailableDevices { get; } = new();

    public J2534DeviceInfo? SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
    }

    public string FirmwareVersion
    {
        get => _firmwareVersion;
        set => SetProperty(ref _firmwareVersion, value);
    }

    public string DllVersion
    {
        get => _dllVersion;
        set => SetProperty(ref _dllVersion, value);
    }

    public string ApiVersion
    {
        get => _apiVersion;
        set => SetProperty(ref _apiVersion, value);
    }

    public string BatteryVoltage
    {
        get => _batteryVoltage;
        set => SetProperty(ref _batteryVoltage, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public ICommand ScanDevicesCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }

    public ConnectionViewModel(J2534Api api, SessionLogger logger,
        Action<J2534Device> onConnected, Action onDisconnected)
    {
        _api = api;
        _logger = logger;
        _onConnected = onConnected;
        _onDisconnected = onDisconnected;

        ScanDevicesCommand = new RelayCommand(ScanDevices);
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => SelectedDevice != null && !IsConnected);
        DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);
    }

    private void ScanDevices()
    {
        AvailableDevices.Clear();
        _logger.LogInfo("Skanowanie urzadzen J2534...");

        try
        {
            var devices = J2534Api.FindDevices();
            foreach (var device in devices)
            {
                AvailableDevices.Add(device);
                _logger.LogInfo($"Znaleziono: {device.Name} ({device.Vendor})");
            }

            if (devices.Count == 0)
                _logger.LogWarning("Nie znaleziono urzadzen J2534. Sprawdz czy sterownik SM3 jest zainstalowany.");
            else if (devices.Count == 1)
                SelectedDevice = devices[0];
        }
        catch (Exception ex)
        {
            _logger.LogError("Blad skanowania urzadzen", ex);
        }
    }

    private async Task ConnectAsync()
    {
        if (SelectedDevice == null) return;

        ConnectionStatus = "Laczenie...";
        _logger.LogInfo($"Laczenie z: {SelectedDevice.Name}...");

        try
        {
            await Task.Run(() =>
            {
                _device = new J2534Device(_api, SelectedDevice);
                _device.Open();
            });

            IsConnected = true;
            ConnectionStatus = "Polaczony";

            try
            {
                var (fw, dll, api) = _device!.ReadVersion();
                FirmwareVersion = fw;
                DllVersion = dll;
                ApiVersion = api;
                _logger.LogInfo($"Firmware: {fw}, DLL: {dll}, API: {api}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Nie mozna odczytac wersji: {ex.Message}");
            }

            try
            {
                var voltage = _device!.ReadBatteryVoltage();
                BatteryVoltage = $"{voltage / 1000.0:F1} V";
                _logger.LogInfo($"Napiecie akumulatora: {BatteryVoltage}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Nie mozna odczytac napiecia: {ex.Message}");
            }

            _onConnected(_device!);
        }
        catch (Exception ex)
        {
            ConnectionStatus = "Blad polaczenia";
            _logger.LogError($"Blad polaczenia z {SelectedDevice.Name}", ex);
            _device?.Dispose();
            _device = null;
        }
    }

    private void Disconnect()
    {
        try
        {
            _device?.Dispose();
            _device = null;
            IsConnected = false;
            ConnectionStatus = "Rozlaczony";
            FirmwareVersion = "";
            DllVersion = "";
            ApiVersion = "";
            BatteryVoltage = "";
            _onDisconnected();
        }
        catch (Exception ex)
        {
            _logger.LogError("Blad rozlaczania", ex);
        }
    }
}
