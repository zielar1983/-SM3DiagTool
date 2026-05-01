using System.Collections.ObjectModel;
using System.Windows.Input;
using SM3DiagTool.Helpers;
using SM3DiagTool.J2534;
using SM3DiagTool.Logging;
using SM3DiagTool.Models;
using SM3DiagTool.Protocols;

namespace SM3DiagTool.ViewModels;

public class EcuInfoViewModel : ObservableObject
{
    private readonly SessionLogger _logger;
    private J2534Device? _device;
    private J2534Api? _api;
    private string _statusText = "Polacz urzadzenie";
    private bool _isReading;
    private string _vin = "";
    private string _ecuName = "";
    private string _manufacturer = "";
    private string _softwareVersion = "";
    private string _hardwareVersion = "";
    private string _serialNumber = "";
    private string _partNumber = "";
    private string _calibrationId = "";

    public string Vin { get => _vin; set => SetProperty(ref _vin, value); }
    public string EcuName { get => _ecuName; set => SetProperty(ref _ecuName, value); }
    public string Manufacturer { get => _manufacturer; set => SetProperty(ref _manufacturer, value); }
    public string SoftwareVersion { get => _softwareVersion; set => SetProperty(ref _softwareVersion, value); }
    public string HardwareVersion { get => _hardwareVersion; set => SetProperty(ref _hardwareVersion, value); }
    public string SerialNumber { get => _serialNumber; set => SetProperty(ref _serialNumber, value); }
    public string PartNumber { get => _partNumber; set => SetProperty(ref _partNumber, value); }
    public string CalibrationId { get => _calibrationId; set => SetProperty(ref _calibrationId, value); }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsReading
    {
        get => _isReading;
        set => SetProperty(ref _isReading, value);
    }

    public ObservableCollection<EcuInformation> DetectedEcus { get; } = new();

    public ICommand ReadInfoCommand { get; }
    public ICommand ScanEcusCommand { get; }
    public ICommand ReadVinObdCommand { get; }

    public EcuInfoViewModel(SessionLogger logger)
    {
        _logger = logger;
        ReadInfoCommand = new AsyncRelayCommand(ReadInfoAsync, () => _device != null && !IsReading);
        ScanEcusCommand = new AsyncRelayCommand(ScanEcusAsync, () => _device != null && !IsReading);
        ReadVinObdCommand = new AsyncRelayCommand(ReadVinObdAsync, () => _device != null && !IsReading);
    }

    public void SetDevice(J2534Device device, J2534Api api)
    {
        _device = device;
        _api = api;
        StatusText = "Gotowy do odczytu informacji o ECU";
    }

    public void ClearDevice()
    {
        _device = null;
        _api = null;
        ClearFields();
        DetectedEcus.Clear();
        StatusText = "Polacz urzadzenie";
    }

    private void ClearFields()
    {
        Vin = ""; EcuName = ""; Manufacturer = "";
        SoftwareVersion = ""; HardwareVersion = "";
        SerialNumber = ""; PartNumber = ""; CalibrationId = "";
    }

    private async Task ReadInfoAsync()
    {
        if (_device == null) return;

        IsReading = true;
        StatusText = "Odczyt informacji o ECU (UDS)...";
        ClearFields();
        _logger.LogInfo("Odczyt informacji o ECU...");

        try
        {
            var info = await Task.Run(() =>
            {
                using var channel = Uds.OpenChannel(_device, 0x7E0, 0x7E8);
                var uds = new Uds(channel);
                uds.StartSession(Uds.SessionType.ExtendedDiagnostic);
                return uds.ReadEcuInfo();
            });

            Vin = info.Vin;
            EcuName = info.EcuName;
            Manufacturer = info.Manufacturer;
            SoftwareVersion = info.SoftwareVersion;
            HardwareVersion = info.HardwareVersion;
            SerialNumber = info.SerialNumber;
            PartNumber = info.PartNumber;
            CalibrationId = info.CalibrationId;

            StatusText = "Odczyt zakonczony";
            _logger.LogInfo($"ECU Info - VIN: {info.Vin}, ECU: {info.EcuName}");
        }
        catch (Exception ex)
        {
            StatusText = $"Blad odczytu: {ex.Message}";
            _logger.LogError("Blad odczytu ECU info", ex);
        }
        finally
        {
            IsReading = false;
        }
    }

    private async Task ReadVinObdAsync()
    {
        if (_device == null) return;

        IsReading = true;
        StatusText = "Odczyt VIN (OBD-II)...";

        try
        {
            var (vin, calId) = await Task.Run(() =>
            {
                using var channel = ObdII.OpenChannel(_device);
                var obd = new ObdII(channel);
                return (obd.ReadVin(), obd.ReadCalibrationId());
            });

            Vin = vin;
            CalibrationId = calId;
            StatusText = string.IsNullOrEmpty(vin) ? "Nie mozna odczytac VIN" : $"VIN: {vin}";
            _logger.LogInfo($"OBD-II VIN: {vin}, CalID: {calId}");
        }
        catch (Exception ex)
        {
            StatusText = $"Blad odczytu VIN: {ex.Message}";
            _logger.LogError("Blad odczytu VIN", ex);
        }
        finally
        {
            IsReading = false;
        }
    }

    private async Task ScanEcusAsync()
    {
        if (_device == null) return;

        IsReading = true;
        DetectedEcus.Clear();
        StatusText = "Skanowanie ECU na magistrali CAN...";
        _logger.LogInfo("Skanowanie ECU...");

        try
        {
            var ecus = await Task.Run(() =>
            {
                var found = new List<EcuInformation>();

                for (uint addr = 0x7E0; addr <= 0x7EF; addr++)
                {
                    try
                    {
                        uint rxAddr = addr + 8;
                        using var channel = Uds.OpenChannel(_device, addr, rxAddr);
                        var uds = new Uds(channel, addr, rxAddr);

                        var response = uds.SendRequest(Uds.ServiceId.DiagnosticSessionControl,
                            new[] { Uds.SessionType.Default });

                        if (response.Length > 0)
                        {
                            var info = new EcuInformation
                            {
                                DiagAddress = addr,
                                EcuName = $"ECU @ 0x{addr:X3}"
                            };

                            try { info.EcuName = uds.ReadStringDid(Uds.DataIdentifier.SystemName); } catch { }
                            try { info.PartNumber = uds.ReadStringDid(Uds.DataIdentifier.VehicleManufacturerPartNumber); } catch { }

                            found.Add(info);
                        }
                    }
                    catch { }
                }

                return found;
            });

            foreach (var ecu in ecus)
                DetectedEcus.Add(ecu);

            StatusText = $"Znaleziono {ecus.Count} ECU";
            _logger.LogInfo($"Znaleziono {ecus.Count} ECU na magistrali CAN");
        }
        catch (Exception ex)
        {
            StatusText = $"Blad skanowania: {ex.Message}";
            _logger.LogError("Blad skanowania ECU", ex);
        }
        finally
        {
            IsReading = false;
        }
    }
}
