using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using SM3DiagTool.Helpers;
using SM3DiagTool.J2534;
using SM3DiagTool.Logging;
using SM3DiagTool.Models;
using SM3DiagTool.Protocols;

namespace SM3DiagTool.ViewModels;

public class LiveDataViewModel : ObservableObject
{
    private readonly SessionLogger _logger;
    private J2534Device? _device;
    private J2534Api? _api;
    private J2534Channel? _channel;
    private ObdII? _obd;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private string _statusText = "Polacz urzadzenie";
    private int _refreshRate = 500;

    public ObservableCollection<PidData> LivePids { get; } = new();
    public ObservableCollection<PidData> AvailablePids { get; } = new();
    public ObservableCollection<PidData> SelectedPids { get; } = new();

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

    public int RefreshRate
    {
        get => _refreshRate;
        set => SetProperty(ref _refreshRate, value);
    }

    public ICommand ScanPidsCommand { get; }
    public ICommand StartMonitoringCommand { get; }
    public ICommand StopMonitoringCommand { get; }
    public ICommand AddPidCommand { get; }
    public ICommand RemovePidCommand { get; }
    public ICommand AddAllPidsCommand { get; }

    public LiveDataViewModel(SessionLogger logger)
    {
        _logger = logger;
        ScanPidsCommand = new AsyncRelayCommand(ScanPidsAsync, () => _device != null && !IsRunning);
        StartMonitoringCommand = new AsyncRelayCommand(StartMonitoringAsync, () => _device != null && !IsRunning && SelectedPids.Count > 0);
        StopMonitoringCommand = new RelayCommand(StopMonitoring, () => IsRunning);
        AddPidCommand = new RelayCommand(p => AddPid(p as PidData));
        RemovePidCommand = new RelayCommand(p => RemovePid(p as PidData));
        AddAllPidsCommand = new RelayCommand(() =>
        {
            foreach (var pid in AvailablePids.ToList())
                if (!SelectedPids.Any(s => s.Pid == pid.Pid))
                    SelectedPids.Add(pid);
        });
    }

    public void SetDevice(J2534Device device, J2534Api api)
    {
        _device = device;
        _api = api;
        StatusText = "Gotowy - kliknij 'Skanuj PID-y'";
    }

    public void ClearDevice()
    {
        StopMonitoring();
        _device = null;
        _api = null;
        LivePids.Clear();
        AvailablePids.Clear();
        SelectedPids.Clear();
        StatusText = "Polacz urzadzenie";
    }

    private async Task ScanPidsAsync()
    {
        if (_device == null) return;

        StatusText = "Skanowanie dostepnych PID-ow...";
        AvailablePids.Clear();
        _logger.LogInfo("Skanowanie PID-ow...");

        try
        {
            var pids = await Task.Run(() =>
            {
                using var channel = ObdII.OpenChannel(_device);
                var obd = new ObdII(channel);
                return obd.GetAllSupportedPids();
            });

            foreach (var pid in pids)
            {
                if (ObdPids.StandardPids.TryGetValue(pid, out var pidDef))
                    AvailablePids.Add(new PidData
                    {
                        Pid = pidDef.Pid, Name = pidDef.Name, Unit = pidDef.Unit,
                        MinValue = pidDef.MinValue, MaxValue = pidDef.MaxValue, Parser = pidDef.Parser
                    });
                else
                    AvailablePids.Add(new PidData { Pid = pid, Name = $"PID 0x{pid:X2}", Unit = "" });
            }

            StatusText = $"Znaleziono {pids.Count} PID-ow";
            _logger.LogInfo($"Znaleziono {pids.Count} obslugiwanych PID-ow");
        }
        catch (Exception ex)
        {
            StatusText = $"Blad skanowania: {ex.Message}";
            _logger.LogError("Blad skanowania PID-ow", ex);
        }
    }

    private async Task StartMonitoringAsync()
    {
        if (_device == null || SelectedPids.Count == 0) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;
        LivePids.Clear();
        StatusText = "Monitorowanie danych na zywo...";
        _logger.LogInfo("Rozpoczeto monitorowanie danych na zywo");

        try
        {
            _channel = ObdII.OpenChannel(_device);
            _obd = new ObdII(_channel);

            foreach (var pid in SelectedPids)
                LivePids.Add(new PidData { Pid = pid.Pid, Name = pid.Name, Unit = pid.Unit,
                    MinValue = pid.MinValue, MaxValue = pid.MaxValue, Parser = pid.Parser });

            while (!_cts.Token.IsCancellationRequested)
            {
                foreach (var livePid in LivePids)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    try
                    {
                        var data = _obd.ReadPid(livePid.Pid);
                        if (data != null)
                        {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                livePid.Value = data.Value;
                                livePid.Timestamp = DateTime.Now;
                                OnPropertyChanged(nameof(LivePids));
                            });
                        }
                    }
                    catch { }
                }

                await Task.Delay(RefreshRate, _cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusText = $"Blad monitorowania: {ex.Message}";
            _logger.LogError("Blad monitorowania danych", ex);
        }
        finally
        {
            _channel?.Dispose();
            _channel = null;
            _obd = null;
            IsRunning = false;
            StatusText = "Monitorowanie zatrzymane";
        }
    }

    private void StopMonitoring()
    {
        _cts?.Cancel();
        _logger.LogInfo("Zatrzymano monitorowanie danych");
    }

    private void AddPid(PidData? pid)
    {
        if (pid != null && !SelectedPids.Any(p => p.Pid == pid.Pid))
            SelectedPids.Add(pid);
    }

    private void RemovePid(PidData? pid)
    {
        if (pid != null) SelectedPids.Remove(pid);
    }
}
