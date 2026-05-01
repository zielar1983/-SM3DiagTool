using System.Collections.ObjectModel;
using System.Windows.Input;
using SM3DiagTool.Helpers;
using SM3DiagTool.J2534;
using SM3DiagTool.Logging;
using SM3DiagTool.Models;
using SM3DiagTool.Protocols;

namespace SM3DiagTool.ViewModels;

public class DtcViewModel : ObservableObject
{
    private readonly SessionLogger _logger;
    private J2534Device? _device;
    private J2534Api? _api;
    private string _statusText = "Polacz urzadzenie aby odczytac kody bledow";
    private bool _isReading;
    private string _selectedProtocol = "OBD-II";

    public ObservableCollection<DiagnosticTroubleCode> Dtcs { get; } = new();
    public ObservableCollection<string> Protocols { get; } = new() { "OBD-II", "UDS", "KWP2000" };

    public string SelectedProtocol
    {
        get => _selectedProtocol;
        set => SetProperty(ref _selectedProtocol, value);
    }

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

    public ICommand ReadDtcsCommand { get; }
    public ICommand ClearDtcsCommand { get; }
    public ICommand ExportDtcsCommand { get; }

    public DtcViewModel(SessionLogger logger)
    {
        _logger = logger;
        ReadDtcsCommand = new AsyncRelayCommand(ReadDtcsAsync, () => _device != null && !IsReading);
        ClearDtcsCommand = new AsyncRelayCommand(ClearDtcsAsync, () => _device != null && !IsReading);
        ExportDtcsCommand = new RelayCommand(ExportDtcs, () => Dtcs.Count > 0);
    }

    public void SetDevice(J2534Device device, J2534Api api)
    {
        _device = device;
        _api = api;
        StatusText = "Gotowy do odczytu kodow bledow";
    }

    public void ClearDevice()
    {
        _device = null;
        _api = null;
        Dtcs.Clear();
        StatusText = "Polacz urzadzenie aby odczytac kody bledow";
    }

    private async Task ReadDtcsAsync()
    {
        if (_device == null) return;

        IsReading = true;
        StatusText = "Odczytywanie kodow bledow...";
        Dtcs.Clear();
        _logger.LogInfo($"Odczyt DTC ({SelectedProtocol})...");

        try
        {
            var dtcs = await Task.Run(() =>
            {
                switch (SelectedProtocol)
                {
                    case "OBD-II":
                    {
                        using var channel = ObdII.OpenChannel(_device);
                        var obd = new ObdII(channel);
                        return obd.ReadDtcs();
                    }
                    case "UDS":
                    {
                        using var channel = Uds.OpenChannel(_device, 0x7E0, 0x7E8);
                        var uds = new Uds(channel);
                        uds.StartSession(Uds.SessionType.ExtendedDiagnostic);
                        return uds.ReadDtcs();
                    }
                    case "KWP2000":
                    {
                        using var channel = Kwp2000.OpenKLineChannel(_device);
                        var kwp = new Kwp2000(channel);
                        kwp.StartDiagnosticSession();
                        return kwp.ReadDtcs();
                    }
                    default:
                        return new List<DiagnosticTroubleCode>();
                }
            });

            foreach (var dtc in dtcs)
                Dtcs.Add(dtc);

            StatusText = dtcs.Count > 0
                ? $"Znaleziono {dtcs.Count} kodow bledow"
                : "Brak kodow bledow";

            _logger.LogInfo($"Odczytano {dtcs.Count} DTC");
        }
        catch (Exception ex)
        {
            StatusText = $"Blad odczytu: {ex.Message}";
            _logger.LogError("Blad odczytu DTC", ex);
        }
        finally
        {
            IsReading = false;
        }
    }

    private async Task ClearDtcsAsync()
    {
        if (_device == null) return;

        IsReading = true;
        StatusText = "Kasowanie kodow bledow...";
        _logger.LogInfo("Kasowanie DTC...");

        try
        {
            var success = await Task.Run(() =>
            {
                using var channel = ObdII.OpenChannel(_device);
                var obd = new ObdII(channel);
                return obd.ClearDtcs();
            });

            if (success)
            {
                Dtcs.Clear();
                StatusText = "Kody bledow skasowane";
                _logger.LogInfo("DTC skasowane pomyslnie");
            }
            else
            {
                StatusText = "Nie udalo sie skasowac kodow bledow";
                _logger.LogWarning("Kasowanie DTC nie powiodlo sie");
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Blad kasowania: {ex.Message}";
            _logger.LogError("Blad kasowania DTC", ex);
        }
        finally
        {
            IsReading = false;
        }
    }

    private void ExportDtcs()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|Tekst (*.txt)|*.txt",
            DefaultExt = ".csv",
            FileName = $"DTC_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8);
                writer.WriteLine("Kod,Opis,Status,Modul");
                foreach (var dtc in Dtcs)
                    writer.WriteLine($"{dtc.Code},{dtc.Description},{dtc.StatusText},{dtc.Module}");

                _logger.LogInfo($"DTC wyeksportowane do: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Blad eksportu DTC", ex);
            }
        }
    }
}
