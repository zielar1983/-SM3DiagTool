using SM3DiagTool.J2534;
using Serilog;

namespace SM3DiagTool.Protocols;

public class IoControl
{
    private readonly Uds _uds;

    public static class ControlParameter
    {
        public const byte ReturnControlToEcu = 0x00;
        public const byte ResetToDefault = 0x01;
        public const byte FreezeCurrentState = 0x02;
        public const byte ShortTermAdjustment = 0x03;
    }

    public static class CommonIoIdentifiers
    {
        public const ushort EngineIdleSpeed = 0xF000;
        public const ushort FuelInjection = 0xF001;
        public const ushort IgnitionTiming = 0xF002;
        public const ushort ThrottleActuator = 0xF003;
        public const ushort EgrValve = 0xF004;
        public const ushort CoolingFan = 0xF005;
        public const ushort AcClutch = 0xF006;
        public const ushort PurgeSolenoid = 0xF007;
        public const ushort OxygenSensorHeater = 0xF008;
        public const ushort FuelPump = 0xF009;
        public const ushort MilLamp = 0xF00A;
        public const ushort HornRelay = 0xF00B;
        public const ushort HeadlightRelay = 0xF00C;
        public const ushort TailLightRelay = 0xF00D;
        public const ushort BrakeLightRelay = 0xF00E;
        public const ushort DoorLock = 0xF00F;
        public const ushort WindowMotor = 0xF010;
        public const ushort MirrorMotor = 0xF011;
        public const ushort WiperMotor = 0xF012;
        public const ushort WasherPump = 0xF013;
    }

    public IoControl(Uds uds)
    {
        _uds = uds;
    }

    public byte[] ControlOutput(ushort identifier, byte controlParam, byte[]? controlState = null)
    {
        var data = new List<byte>
        {
            (byte)(identifier >> 8), (byte)identifier,
            controlParam
        };
        if (controlState != null)
            data.AddRange(controlState);

        var response = _uds.SendRequest(Uds.ServiceId.InputOutputControlByIdentifier, data.ToArray());
        Log.Information("IO Control: ID=0x{Id:X4}, Param=0x{Param:X2}", identifier, controlParam);
        return response;
    }

    public byte[] ReturnControlToEcu(ushort identifier)
    {
        return ControlOutput(identifier, ControlParameter.ReturnControlToEcu);
    }

    public byte[] ResetToDefault(ushort identifier)
    {
        return ControlOutput(identifier, ControlParameter.ResetToDefault);
    }

    public byte[] FreezeCurrentState(ushort identifier)
    {
        return ControlOutput(identifier, ControlParameter.FreezeCurrentState);
    }

    public byte[] ShortTermAdjustment(ushort identifier, byte[] controlState)
    {
        return ControlOutput(identifier, ControlParameter.ShortTermAdjustment, controlState);
    }

    public void ActivateActuator(ushort identifier, byte value)
    {
        ShortTermAdjustment(identifier, new[] { value });
        Log.Information("Actuator activated: ID=0x{Id:X4}, Value=0x{Value:X2}", identifier, value);
    }

    public void DeactivateActuator(ushort identifier)
    {
        ReturnControlToEcu(identifier);
        Log.Information("Actuator deactivated (returned to ECU): ID=0x{Id:X4}", identifier);
    }

    public byte[] ReadCurrentState(ushort identifier)
    {
        return _uds.ReadDataByIdentifier(identifier);
    }

    public static string GetIdentifierName(ushort identifier)
    {
        return identifier switch
        {
            CommonIoIdentifiers.EngineIdleSpeed => "Obroty biegu jalowego",
            CommonIoIdentifiers.FuelInjection => "Wtrysk paliwa",
            CommonIoIdentifiers.IgnitionTiming => "Kat zaplonu",
            CommonIoIdentifiers.ThrottleActuator => "Silnik przepustnicy",
            CommonIoIdentifiers.EgrValve => "Zawor EGR",
            CommonIoIdentifiers.CoolingFan => "Wentylator chlodnicy",
            CommonIoIdentifiers.AcClutch => "Sprzeglo klimatyzacji",
            CommonIoIdentifiers.PurgeSolenoid => "Zawor odparowania",
            CommonIoIdentifiers.OxygenSensorHeater => "Grzalka sondy lambda",
            CommonIoIdentifiers.FuelPump => "Pompa paliwa",
            CommonIoIdentifiers.MilLamp => "Lampka MIL (check engine)",
            CommonIoIdentifiers.HornRelay => "Przekaznik klaksonu",
            CommonIoIdentifiers.HeadlightRelay => "Przekaznik swiatel przednich",
            CommonIoIdentifiers.TailLightRelay => "Przekaznik swiatel tylnych",
            CommonIoIdentifiers.BrakeLightRelay => "Przekaznik swiatel hamowania",
            CommonIoIdentifiers.DoorLock => "Zamek drzwi",
            CommonIoIdentifiers.WindowMotor => "Silnik szyby",
            CommonIoIdentifiers.MirrorMotor => "Silnik lusterka",
            CommonIoIdentifiers.WiperMotor => "Silnik wycieraczek",
            CommonIoIdentifiers.WasherPump => "Pompa spryskiwaczy",
            _ => $"IO 0x{identifier:X4}"
        };
    }
}

public class RoutineManager
{
    private readonly Uds _uds;

    public static class ControlType
    {
        public const byte StartRoutine = 0x01;
        public const byte StopRoutine = 0x02;
        public const byte RequestResults = 0x03;
    }

    public static class CommonRoutines
    {
        public const ushort EraseMemory = 0xFF00;
        public const ushort CheckProgrammingDependencies = 0xFF01;
        public const ushort CheckProgrammingPreconditions = 0x0203;
        public const ushort RequestDownloadForDiagnostic = 0x0202;
        public const ushort ResetLearnedValues = 0x0301;
        public const ushort ForceDpfRegeneration = 0x0401;
        public const ushort BleedBrakes = 0x0501;
        public const ushort SteeringAngleSensorCalibration = 0x0601;
        public const ushort ThrottleBodyAdaptation = 0x0701;
        public const ushort InjectorCoding = 0x0801;
        public const ushort BatteryRegistration = 0x0901;
        public const ushort OilServiceReset = 0x0A01;
        public const ushort ServiceIntervalReset = 0x0A02;
    }

    public RoutineManager(Uds uds)
    {
        _uds = uds;
    }

    public byte[] StartRoutine(ushort routineId, byte[]? options = null)
    {
        Log.Information("Starting routine: 0x{RoutineId:X4}", routineId);
        return _uds.RoutineControl(ControlType.StartRoutine, routineId, options);
    }

    public byte[] StopRoutine(ushort routineId)
    {
        Log.Information("Stopping routine: 0x{RoutineId:X4}", routineId);
        return _uds.RoutineControl(ControlType.StopRoutine, routineId);
    }

    public byte[] RequestResults(ushort routineId)
    {
        return _uds.RoutineControl(ControlType.RequestResults, routineId);
    }

    public bool ForceDpfRegeneration()
    {
        try
        {
            StartRoutine(CommonRoutines.ForceDpfRegeneration);
            Log.Information("DPF regeneration started");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DPF regeneration failed");
            return false;
        }
    }

    public bool ResetServiceInterval()
    {
        try
        {
            StartRoutine(CommonRoutines.ServiceIntervalReset);
            Log.Information("Service interval reset");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Service interval reset failed");
            return false;
        }
    }

    public bool CalibrateSteeringAngle()
    {
        try
        {
            StartRoutine(CommonRoutines.SteeringAngleSensorCalibration);
            Log.Information("Steering angle calibration started");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Steering angle calibration failed");
            return false;
        }
    }

    public bool AdaptThrottleBody()
    {
        try
        {
            StartRoutine(CommonRoutines.ThrottleBodyAdaptation);
            Log.Information("Throttle body adaptation started");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Throttle body adaptation failed");
            return false;
        }
    }

    public bool RegisterBattery()
    {
        try
        {
            StartRoutine(CommonRoutines.BatteryRegistration);
            Log.Information("Battery registration completed");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Battery registration failed");
            return false;
        }
    }

    public static string GetRoutineName(ushort routineId)
    {
        return routineId switch
        {
            CommonRoutines.EraseMemory => "Kasowanie pamieci",
            CommonRoutines.CheckProgrammingDependencies => "Sprawdzenie zaleznosci programowania",
            CommonRoutines.ResetLearnedValues => "Reset wartosci wyuczonych",
            CommonRoutines.ForceDpfRegeneration => "Wymuszenie regeneracji DPF",
            CommonRoutines.BleedBrakes => "Odpowietrzanie hamulcow",
            CommonRoutines.SteeringAngleSensorCalibration => "Kalibracja czujnika kata skretu",
            CommonRoutines.ThrottleBodyAdaptation => "Adaptacja przepustnicy",
            CommonRoutines.InjectorCoding => "Kodowanie wtryskiwaczy",
            CommonRoutines.BatteryRegistration => "Rejestracja akumulatora",
            CommonRoutines.OilServiceReset => "Reset przegladu oleju",
            CommonRoutines.ServiceIntervalReset => "Reset intervalu serwisowego",
            _ => $"Procedura 0x{routineId:X4}"
        };
    }
}
