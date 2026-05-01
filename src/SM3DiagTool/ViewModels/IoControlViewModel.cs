using System.Collections.ObjectModel;
using System.Windows.Input;
using SM3DiagTool.Helpers;
using SM3DiagTool.J2534;
using SM3DiagTool.Logging;
using SM3DiagTool.Protocols;

namespace SM3DiagTool.ViewModels;

public class IoControlViewModel : ObservableObject
{
    private readonly SessionLogger _logger;
    private J2534Device? _device;
    private J2534Api? _api;
    private string _statusText = "Polacz urzadzenie";
    private string _selectedActuatorId = "F000";
    private byte _controlValue = 0xFF;

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public string SelectedActuatorId { get => _selectedActuatorId; set => SetProperty(ref _selectedActuatorId, value); }
    public byte ControlValue { get => _controlValue; set => SetProperty(ref _controlValue, value); }

    public ObservableCollection<ActuatorInfo> Actuators { get; } = new();
    public ObservableCollection<RoutineInfo> Routines { get; } = new();
    public ObservableCollection<string> ActivityLog { get; } = new();

    public ICommand ActivateCommand { get; }
    public ICommand DeactivateCommand { get; }
    public ICommand FreezeCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand StartRoutineCommand { get; }
    public ICommand StopRoutineCommand { get; }
    public ICommand GetResultsCommand { get; }

    public IoControlViewModel(SessionLogger logger)
    {
        _logger = logger;
        ActivateCommand = new AsyncRelayCommand(ActivateAsync, () => _device != null);
        DeactivateCommand = new AsyncRelayCommand(DeactivateAsync, () => _device != null);
        FreezeCommand = new AsyncRelayCommand(FreezeAsync, () => _device != null);
        ResetCommand = new AsyncRelayCommand(ResetAsync, () => _device != null);
        StartRoutineCommand = new AsyncRelayCommand(StartRoutineAsync, () => _device != null);
        StopRoutineCommand = new AsyncRelayCommand(StopRoutineAsync, () => _device != null);
        GetResultsCommand = new AsyncRelayCommand(GetResultsAsync, () => _device != null);

        InitializeActuators();
        InitializeRoutines();
    }

    private void InitializeActuators()
    {
        Actuators.Add(new ActuatorInfo(0xF005, "Wentylator chlodnicy"));
        Actuators.Add(new ActuatorInfo(0xF006, "Sprzeglo klimatyzacji"));
        Actuators.Add(new ActuatorInfo(0xF009, "Pompa paliwa"));
        Actuators.Add(new ActuatorInfo(0xF00A, "Lampka MIL (check engine)"));
        Actuators.Add(new ActuatorInfo(0xF00B, "Klakson"));
        Actuators.Add(new ActuatorInfo(0xF00C, "Swiatla przednie"));
        Actuators.Add(new ActuatorInfo(0xF00D, "Swiatla tylne"));
        Actuators.Add(new ActuatorInfo(0xF00E, "Swiatla hamowania"));
        Actuators.Add(new ActuatorInfo(0xF00F, "Zamek drzwi"));
        Actuators.Add(new ActuatorInfo(0xF010, "Szyba"));
        Actuators.Add(new ActuatorInfo(0xF012, "Wycieraczki"));
        Actuators.Add(new ActuatorInfo(0xF013, "Spryskiwacze"));
        Actuators.Add(new ActuatorInfo(0xF003, "Silnik przepustnicy"));
        Actuators.Add(new ActuatorInfo(0xF004, "Zawor EGR"));
        Actuators.Add(new ActuatorInfo(0xF008, "Grzalka sondy lambda"));
    }

    private void InitializeRoutines()
    {
        Routines.Add(new RoutineInfo(0x0301, "Reset wartosci wyuczonych"));
        Routines.Add(new RoutineInfo(0x0401, "Wymuszenie regeneracji DPF"));
        Routines.Add(new RoutineInfo(0x0501, "Odpowietrzanie hamulcow"));
        Routines.Add(new RoutineInfo(0x0601, "Kalibracja czujnika kata skretu"));
        Routines.Add(new RoutineInfo(0x0701, "Adaptacja przepustnicy"));
        Routines.Add(new RoutineInfo(0x0801, "Kodowanie wtryskiwaczy"));
        Routines.Add(new RoutineInfo(0x0901, "Rejestracja akumulatora"));
        Routines.Add(new RoutineInfo(0x0A01, "Reset przegladu oleju"));
        Routines.Add(new RoutineInfo(0x0A02, "Reset intervalu serwisowego"));
    }

    public void SetDevice(J2534Device device, J2534Api api)
    {
        _device = device;
        _api = api;
        StatusText = "Gotowy do sterowania";
    }

    public void ClearDevice()
    {
        _device = null;
        _api = null;
        StatusText = "Polacz urzadzenie";
    }

    private async Task ActivateAsync()
    {
        await ExecuteIoControl("Aktywacja", id =>
        {
            return (uds, io) =>
            {
                io.ActivateActuator(id, ControlValue);
                return $"Aktywowano 0x{id:X4} = 0x{ControlValue:X2}";
            };
        });
    }

    private async Task DeactivateAsync()
    {
        await ExecuteIoControl("Dezaktywacja", id =>
        {
            return (uds, io) =>
            {
                io.DeactivateActuator(id);
                return $"Dezaktywowano 0x{id:X4}";
            };
        });
    }

    private async Task FreezeAsync()
    {
        await ExecuteIoControl("Zamrozenie", id =>
        {
            return (uds, io) =>
            {
                io.FreezeCurrentState(id);
                return $"Zamrozono stan 0x{id:X4}";
            };
        });
    }

    private async Task ResetAsync()
    {
        await ExecuteIoControl("Reset", id =>
        {
            return (uds, io) =>
            {
                io.ReturnControlToEcu(id);
                return $"Przywrocono do ECU 0x{id:X4}";
            };
        });
    }

    private async Task ExecuteIoControl(string operation, Func<ushort, Func<Uds, IoControl, string>> actionBuilder)
    {
        if (_device == null) return;

        try
        {
            var id = Convert.ToUInt16(SelectedActuatorId, 16);
            var action = actionBuilder(id);

            var result = await Task.Run(() =>
            {
                using var channel = Uds.OpenChannel(_device, 0x7E0, 0x7E8);
                var uds = new Uds(channel);
                uds.StartSession(Uds.SessionType.ExtendedDiagnostic);
                var io = new IoControl(uds);
                return action(uds, io);
            });

            StatusText = result;
            AddLog($"{operation}: {result}");
        }
        catch (Exception ex)
        {
            StatusText = $"Blad: {ex.Message}";
            AddLog($"Blad {operation}: {ex.Message}");
            _logger.LogError($"IO Control {operation} error", ex);
        }
    }

    private RoutineInfo? _selectedRoutine;
    public RoutineInfo? SelectedRoutine
    {
        get => _selectedRoutine;
        set => SetProperty(ref _selectedRoutine, value);
    }

    private async Task StartRoutineAsync()
    {
        if (_device == null || SelectedRoutine == null) return;

        try
        {
            var routineId = SelectedRoutine.Id;
            await Task.Run(() =>
            {
                using var channel = Uds.OpenChannel(_device, 0x7E0, 0x7E8);
                var uds = new Uds(channel);
                uds.StartSession(Uds.SessionType.ExtendedDiagnostic);

                var security = new SecurityManager(uds);
                security.UnlockLevel(0x01);

                var routine = new RoutineManager(uds);
                routine.StartRoutine(routineId);
            });

            StatusText = $"Procedura 0x{routineId:X4} uruchomiona";
            AddLog($"Start: {SelectedRoutine.Name}");
        }
        catch (Exception ex)
        {
            StatusText = $"Blad: {ex.Message}";
            AddLog($"Blad procedury: {ex.Message}");
        }
    }

    private async Task StopRoutineAsync()
    {
        if (_device == null || SelectedRoutine == null) return;

        try
        {
            var routineId = SelectedRoutine.Id;
            await Task.Run(() =>
            {
                using var channel = Uds.OpenChannel(_device, 0x7E0, 0x7E8);
                var uds = new Uds(channel);
                var routine = new RoutineManager(uds);
                routine.StopRoutine(routineId);
            });

            StatusText = $"Procedura zatrzymana";
            AddLog($"Stop: {SelectedRoutine.Name}");
        }
        catch (Exception ex)
        {
            StatusText = $"Blad: {ex.Message}";
        }
    }

    private async Task GetResultsAsync()
    {
        if (_device == null || SelectedRoutine == null) return;

        try
        {
            var routineId = SelectedRoutine.Id;
            var result = await Task.Run(() =>
            {
                using var channel = Uds.OpenChannel(_device, 0x7E0, 0x7E8);
                var uds = new Uds(channel);
                var routine = new RoutineManager(uds);
                return routine.RequestResults(routineId);
            });

            var hex = string.Join(" ", result.Select(b => $"{b:X2}"));
            StatusText = $"Wynik: {hex}";
            AddLog($"Wynik {SelectedRoutine.Name}: {hex}");
        }
        catch (Exception ex)
        {
            StatusText = $"Blad: {ex.Message}";
        }
    }

    private void AddLog(string message)
    {
        ActivityLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        if (ActivityLog.Count > 100) ActivityLog.RemoveAt(0);
    }
}

public class ActuatorInfo
{
    public ushort Id { get; }
    public string Name { get; }
    public string IdHex => $"0x{Id:X4}";
    public string DisplayName => $"{Name} ({IdHex})";

    public ActuatorInfo(ushort id, string name)
    {
        Id = id;
        Name = name;
    }
}

public class RoutineInfo
{
    public ushort Id { get; }
    public string Name { get; }
    public string IdHex => $"0x{Id:X4}";
    public string DisplayName => $"{Name} ({IdHex})";

    public RoutineInfo(ushort id, string name)
    {
        Id = id;
        Name = name;
    }
}
