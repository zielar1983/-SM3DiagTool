using System.Runtime.InteropServices;
using Microsoft.Win32;
using Serilog;

namespace SM3DiagTool.J2534;

public sealed class J2534Api : IDisposable
{
    private IntPtr _libraryHandle;
    private bool _disposed;
    private readonly object _lock = new();

    private delegate int PassThruOpenDelegate(IntPtr name, out uint deviceId);
    private delegate int PassThruCloseDelegate(uint deviceId);
    private delegate int PassThruConnectDelegate(uint deviceId, uint protocolId, uint flags, uint baudRate, out uint channelId);
    private delegate int PassThruDisconnectDelegate(uint channelId);
    private delegate int PassThruReadMsgsDelegate(uint channelId, IntPtr msgs, ref uint numMsgs, uint timeout);
    private delegate int PassThruWriteMsgsDelegate(uint channelId, IntPtr msgs, ref uint numMsgs, uint timeout);
    private delegate int PassThruStartPeriodicMsgDelegate(uint channelId, IntPtr msg, out uint msgId, uint timeInterval);
    private delegate int PassThruStopPeriodicMsgDelegate(uint channelId, uint msgId);
    private delegate int PassThruStartMsgFilterDelegate(uint channelId, uint filterType, IntPtr maskMsg, IntPtr patternMsg, IntPtr flowControlMsg, out uint filterId);
    private delegate int PassThruStopMsgFilterDelegate(uint channelId, uint filterId);
    private delegate int PassThruSetProgrammingVoltageDelegate(uint deviceId, uint pinNumber, uint voltage);
    private delegate int PassThruReadVersionDelegate(uint deviceId, IntPtr firmwareVersion, IntPtr dllVersion, IntPtr apiVersion);
    private delegate int PassThruGetLastErrorDelegate(IntPtr errorDescription);
    private delegate int PassThruIoctlDelegate(uint channelId, uint ioctlId, IntPtr input, IntPtr output);

    private PassThruOpenDelegate? _passThruOpen;
    private PassThruCloseDelegate? _passThruClose;
    private PassThruConnectDelegate? _passThruConnect;
    private PassThruDisconnectDelegate? _passThruDisconnect;
    private PassThruReadMsgsDelegate? _passThruReadMsgs;
    private PassThruWriteMsgsDelegate? _passThruWriteMsgs;
    private PassThruStartPeriodicMsgDelegate? _passThruStartPeriodicMsg;
    private PassThruStopPeriodicMsgDelegate? _passThruStopPeriodicMsg;
    private PassThruStartMsgFilterDelegate? _passThruStartMsgFilter;
    private PassThruStopMsgFilterDelegate? _passThruStopMsgFilter;
    private PassThruSetProgrammingVoltageDelegate? _passThruSetProgrammingVoltage;
    private PassThruReadVersionDelegate? _passThruReadVersion;
    private PassThruGetLastErrorDelegate? _passThruGetLastError;
    private PassThruIoctlDelegate? _passThruIoctl;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    public bool IsLoaded => _libraryHandle != IntPtr.Zero;

    public static List<J2534DeviceInfo> FindDevices()
    {
        var devices = new List<J2534DeviceInfo>();
        var registryPaths = new[]
        {
            @"SOFTWARE\PassThruSupport.04.04",
            @"SOFTWARE\WOW6432Node\PassThruSupport.04.04"
        };

        foreach (var regPath in registryPaths)
        {
            try
            {
                using var baseKey = Registry.LocalMachine.OpenSubKey(regPath);
                if (baseKey == null) continue;

                foreach (var subKeyName in baseKey.GetSubKeyNames())
                {
                    try
                    {
                        using var deviceKey = baseKey.OpenSubKey(subKeyName);
                        if (deviceKey == null) continue;

                        var device = new J2534DeviceInfo
                        {
                            Name = deviceKey.GetValue("Name")?.ToString() ?? subKeyName,
                            Vendor = deviceKey.GetValue("Vendor")?.ToString() ?? "Unknown",
                            DllPath = deviceKey.GetValue("FunctionLibrary")?.ToString() ?? string.Empty,
                            ConfigApp = deviceKey.GetValue("ConfigApplication")?.ToString() ?? string.Empty,
                        };

                        if (string.IsNullOrEmpty(device.DllPath)) continue;

                        var protocolMap = new Dictionary<string, J2534Protocol>
                        {
                            ["CAN"] = J2534Protocol.CAN,
                            ["ISO15765"] = J2534Protocol.ISO15765,
                            ["ISO14230"] = J2534Protocol.ISO14230,
                            ["ISO9141"] = J2534Protocol.ISO9141,
                            ["J1850VPW"] = J2534Protocol.J1850VPW,
                            ["J1850PWM"] = J2534Protocol.J1850PWM,
                            ["SCI_A_ENGINE"] = J2534Protocol.SCI_A_ENGINE,
                            ["SCI_A_TRANS"] = J2534Protocol.SCI_A_TRANS,
                            ["SCI_B_ENGINE"] = J2534Protocol.SCI_B_ENGINE,
                            ["SCI_B_TRANS"] = J2534Protocol.SCI_B_TRANS,
                        };

                        foreach (var (key, protocol) in protocolMap)
                        {
                            var val = deviceKey.GetValue(key);
                            if (val is int intVal && intVal == 1)
                                device.SupportedProtocols.Add(protocol);
                        }

                        if (!devices.Any(d => d.DllPath == device.DllPath))
                            devices.Add(device);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error reading J2534 device registry key: {SubKey}", subKeyName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error scanning J2534 registry path: {Path}", regPath);
            }
        }

        return devices;
    }

    public void LoadLibrary(string dllPath)
    {
        lock (_lock)
        {
            if (IsLoaded)
                throw new J2534Exception("Library already loaded. Call Dispose() first.");

            _libraryHandle = LoadLibrary(dllPath);
            if (_libraryHandle == IntPtr.Zero)
                throw new J2534Exception($"Failed to load J2534 DLL: {dllPath}. Error: {Marshal.GetLastWin32Error()}");

            LoadFunction("PassThruOpen", out _passThruOpen);
            LoadFunction("PassThruClose", out _passThruClose);
            LoadFunction("PassThruConnect", out _passThruConnect);
            LoadFunction("PassThruDisconnect", out _passThruDisconnect);
            LoadFunction("PassThruReadMsgs", out _passThruReadMsgs);
            LoadFunction("PassThruWriteMsgs", out _passThruWriteMsgs);
            LoadFunction("PassThruStartPeriodicMsg", out _passThruStartPeriodicMsg);
            LoadFunction("PassThruStopPeriodicMsg", out _passThruStopPeriodicMsg);
            LoadFunction("PassThruStartMsgFilter", out _passThruStartMsgFilter);
            LoadFunction("PassThruStopMsgFilter", out _passThruStopMsgFilter);
            LoadFunction("PassThruSetProgrammingVoltage", out _passThruSetProgrammingVoltage);
            LoadFunction("PassThruReadVersion", out _passThruReadVersion);
            LoadFunction("PassThruGetLastError", out _passThruGetLastError);
            LoadFunction("PassThruIoctl", out _passThruIoctl);

            Log.Information("J2534 DLL loaded: {DllPath}", dllPath);
        }
    }

    private void LoadFunction<T>(string name, out T? func) where T : Delegate
    {
        var ptr = GetProcAddress(_libraryHandle, name);
        func = ptr != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<T>(ptr) : default;
        if (func == null)
            Log.Warning("J2534 function not found: {Function}", name);
    }

    private void EnsureDelegate<T>(T? func, string name) where T : Delegate
    {
        if (func == null)
            throw new J2534Exception($"J2534 function '{name}' not available. DLL may not support this operation.");
    }

    public uint Open(string? deviceName = null)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruOpen, "PassThruOpen");
            var namePtr = IntPtr.Zero;
            try
            {
                namePtr = deviceName != null ? Marshal.StringToHGlobalAnsi(deviceName) : IntPtr.Zero;
                var result = (J2534Error)_passThruOpen!(namePtr, out uint deviceId);
                CheckResult(result, "PassThruOpen");
                Log.Information("Device opened. DeviceID={DeviceId}", deviceId);
                return deviceId;
            }
            finally
            {
                if (namePtr != IntPtr.Zero) Marshal.FreeHGlobal(namePtr);
            }
        }
    }

    public void Close(uint deviceId)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruClose, "PassThruClose");
            var result = (J2534Error)_passThruClose!(deviceId);
            CheckResult(result, "PassThruClose");
            Log.Information("Device closed. DeviceID={DeviceId}", deviceId);
        }
    }

    public uint Connect(uint deviceId, J2534Protocol protocol, J2534ConnectFlag flags, uint baudRate)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruConnect, "PassThruConnect");
            var result = (J2534Error)_passThruConnect!(deviceId, (uint)protocol, (uint)flags, baudRate, out uint channelId);
            CheckResult(result, "PassThruConnect");
            Log.Information("Channel opened. ChannelID={ChannelId}, Protocol={Protocol}, BaudRate={BaudRate}",
                channelId, protocol, baudRate);
            return channelId;
        }
    }

    public void Disconnect(uint channelId)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruDisconnect, "PassThruDisconnect");
            var result = (J2534Error)_passThruDisconnect!(channelId);
            CheckResult(result, "PassThruDisconnect");
            Log.Information("Channel closed. ChannelID={ChannelId}", channelId);
        }
    }

    public PassThruMsg[] ReadMsgs(uint channelId, uint numMsgs, uint timeout)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruReadMsgs, "PassThruReadMsgs");
            var msgSize = Marshal.SizeOf<PassThruMsg>();
            var bufferPtr = Marshal.AllocHGlobal(msgSize * (int)numMsgs);
            try
            {
                for (int i = 0; i < numMsgs; i++)
                {
                    var msg = new PassThruMsg { Data = new byte[4128] };
                    Marshal.StructureToPtr(msg, bufferPtr + i * msgSize, false);
                }

                uint count = numMsgs;
                var result = (J2534Error)_passThruReadMsgs!(channelId, bufferPtr, ref count, timeout);
                if (result == J2534Error.ERR_BUFFER_EMPTY || result == J2534Error.ERR_TIMEOUT)
                    return Array.Empty<PassThruMsg>();

                CheckResult(result, "PassThruReadMsgs");

                var messages = new PassThruMsg[count];
                for (int i = 0; i < count; i++)
                    messages[i] = Marshal.PtrToStructure<PassThruMsg>(bufferPtr + i * msgSize);

                return messages;
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }
    }

    public void WriteMsgs(uint channelId, PassThruMsg[] msgs, uint timeout)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruWriteMsgs, "PassThruWriteMsgs");
            var msgSize = Marshal.SizeOf<PassThruMsg>();
            var bufferPtr = Marshal.AllocHGlobal(msgSize * msgs.Length);
            try
            {
                for (int i = 0; i < msgs.Length; i++)
                    Marshal.StructureToPtr(msgs[i], bufferPtr + i * msgSize, false);

                uint count = (uint)msgs.Length;
                var result = (J2534Error)_passThruWriteMsgs!(channelId, bufferPtr, ref count, timeout);
                CheckResult(result, "PassThruWriteMsgs");
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }
    }

    public uint StartMsgFilter(uint channelId, J2534FilterType filterType,
        PassThruMsg? maskMsg, PassThruMsg? patternMsg, PassThruMsg? flowControlMsg)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruStartMsgFilter, "PassThruStartMsgFilter");
            var maskPtr = IntPtr.Zero;
            var patternPtr = IntPtr.Zero;
            var flowPtr = IntPtr.Zero;
            try
            {
                maskPtr = AllocMsg(maskMsg);
                patternPtr = AllocMsg(patternMsg);
                flowPtr = AllocMsg(flowControlMsg);
                var result = (J2534Error)_passThruStartMsgFilter!(channelId, (uint)filterType,
                    maskPtr, patternPtr, flowPtr, out uint filterId);
                CheckResult(result, "PassThruStartMsgFilter");
                return filterId;
            }
            finally
            {
                FreeMsg(maskPtr);
                FreeMsg(patternPtr);
                FreeMsg(flowPtr);
            }
        }
    }

    public void StopMsgFilter(uint channelId, uint filterId)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruStopMsgFilter, "PassThruStopMsgFilter");
            var result = (J2534Error)_passThruStopMsgFilter!(channelId, filterId);
            CheckResult(result, "PassThruStopMsgFilter");
        }
    }

    public uint StartPeriodicMsg(uint channelId, PassThruMsg msg, uint timeInterval)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruStartPeriodicMsg, "PassThruStartPeriodicMsg");
            var msgPtr = IntPtr.Zero;
            try
            {
                msgPtr = AllocMsg(msg);
                var result = (J2534Error)_passThruStartPeriodicMsg!(channelId, msgPtr, out uint msgId, timeInterval);
                CheckResult(result, "PassThruStartPeriodicMsg");
                return msgId;
            }
            finally
            {
                FreeMsg(msgPtr);
            }
        }
    }

    public void StopPeriodicMsg(uint channelId, uint msgId)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruStopPeriodicMsg, "PassThruStopPeriodicMsg");
            var result = (J2534Error)_passThruStopPeriodicMsg!(channelId, msgId);
            CheckResult(result, "PassThruStopPeriodicMsg");
        }
    }

    public void SetProgrammingVoltage(uint deviceId, uint pinNumber, uint voltage)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruSetProgrammingVoltage, "PassThruSetProgrammingVoltage");
            var result = (J2534Error)_passThruSetProgrammingVoltage!(deviceId, pinNumber, voltage);
            CheckResult(result, "PassThruSetProgrammingVoltage");
        }
    }

    public (string firmware, string dll, string api) ReadVersion(uint deviceId)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruReadVersion, "PassThruReadVersion");
            var fwPtr = IntPtr.Zero;
            var dllPtr = IntPtr.Zero;
            var apiPtr = IntPtr.Zero;
            try
            {
                fwPtr = Marshal.AllocHGlobal(80);
                dllPtr = Marshal.AllocHGlobal(80);
                apiPtr = Marshal.AllocHGlobal(80);
                var result = (J2534Error)_passThruReadVersion!(deviceId, fwPtr, dllPtr, apiPtr);
                CheckResult(result, "PassThruReadVersion");
                return (
                    Marshal.PtrToStringAnsi(fwPtr) ?? "",
                    Marshal.PtrToStringAnsi(dllPtr) ?? "",
                    Marshal.PtrToStringAnsi(apiPtr) ?? ""
                );
            }
            finally
            {
                if (fwPtr != IntPtr.Zero) Marshal.FreeHGlobal(fwPtr);
                if (dllPtr != IntPtr.Zero) Marshal.FreeHGlobal(dllPtr);
                if (apiPtr != IntPtr.Zero) Marshal.FreeHGlobal(apiPtr);
            }
        }
    }

    public string GetLastError()
    {
        if (!IsLoaded || _passThruGetLastError == null) return "Library not loaded";
        var ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(256);
            _passThruGetLastError(ptr);
            return Marshal.PtrToStringAnsi(ptr) ?? "Unknown error";
        }
        finally
        {
            if (ptr != IntPtr.Zero) Marshal.FreeHGlobal(ptr);
        }
    }

    public void Ioctl(uint channelId, J2534Ioctl ioctlId, IntPtr input = default, IntPtr output = default)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruIoctl, "PassThruIoctl");
            var result = (J2534Error)_passThruIoctl!(channelId, (uint)ioctlId, input, output);
            CheckResult(result, $"PassThruIoctl({ioctlId})");
        }
    }

    public uint ReadBatteryVoltage(uint deviceId)
    {
        lock (_lock)
        {
            EnsureLoaded();
            EnsureDelegate(_passThruIoctl, "PassThruIoctl");
            var outputPtr = IntPtr.Zero;
            try
            {
                outputPtr = Marshal.AllocHGlobal(4);
                var result = (J2534Error)_passThruIoctl!(deviceId, (uint)J2534Ioctl.READ_VBATT, IntPtr.Zero, outputPtr);
                CheckResult(result, "ReadBatteryVoltage");
                return (uint)Marshal.ReadInt32(outputPtr);
            }
            finally
            {
                if (outputPtr != IntPtr.Zero) Marshal.FreeHGlobal(outputPtr);
            }
        }
    }

    public void ClearRxBuffer(uint channelId)
    {
        Ioctl(channelId, J2534Ioctl.CLEAR_RX_BUFFER);
    }

    public void ClearTxBuffer(uint channelId)
    {
        Ioctl(channelId, J2534Ioctl.CLEAR_TX_BUFFER);
    }

    public void ClearMsgFilters(uint channelId)
    {
        Ioctl(channelId, J2534Ioctl.CLEAR_MSG_FILTERS);
    }

    public void SetConfig(uint channelId, J2534ConfigParameter parameter, uint value)
    {
        lock (_lock)
        {
            EnsureLoaded();
            var config = new SConfig { Parameter = (uint)parameter, Value = value };
            var configPtr = IntPtr.Zero;
            var listPtr = IntPtr.Zero;
            try
            {
                configPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SConfig>());
                listPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SConfigList>());
                Marshal.StructureToPtr(config, configPtr, false);
                var configList = new SConfigList { NumOfParams = 1, ConfigPtr = configPtr };
                Marshal.StructureToPtr(configList, listPtr, false);
                Ioctl(channelId, J2534Ioctl.SET_CONFIG, listPtr);
            }
            finally
            {
                if (configPtr != IntPtr.Zero) Marshal.FreeHGlobal(configPtr);
                if (listPtr != IntPtr.Zero) Marshal.FreeHGlobal(listPtr);
            }
        }
    }

    public uint GetConfig(uint channelId, J2534ConfigParameter parameter)
    {
        lock (_lock)
        {
            EnsureLoaded();
            var config = new SConfig { Parameter = (uint)parameter, Value = 0 };
            var configPtr = IntPtr.Zero;
            var listPtr = IntPtr.Zero;
            try
            {
                configPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SConfig>());
                listPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SConfigList>());
                Marshal.StructureToPtr(config, configPtr, false);
                var configList = new SConfigList { NumOfParams = 1, ConfigPtr = configPtr };
                Marshal.StructureToPtr(configList, listPtr, false);
                Ioctl(channelId, J2534Ioctl.GET_CONFIG, IntPtr.Zero, listPtr);
                var resultConfig = Marshal.PtrToStructure<SConfig>(configPtr);
                return resultConfig.Value;
            }
            finally
            {
                if (configPtr != IntPtr.Zero) Marshal.FreeHGlobal(configPtr);
                if (listPtr != IntPtr.Zero) Marshal.FreeHGlobal(listPtr);
            }
        }
    }

    private IntPtr AllocMsg(PassThruMsg? msg)
    {
        if (msg == null) return IntPtr.Zero;
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<PassThruMsg>());
        Marshal.StructureToPtr(msg.Value, ptr, false);
        return ptr;
    }

    private void FreeMsg(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero) Marshal.FreeHGlobal(ptr);
    }

    private void EnsureLoaded()
    {
        if (!IsLoaded)
            throw new J2534Exception("J2534 library not loaded. Call LoadLibrary() first.");
    }

    private void CheckResult(J2534Error result, string function)
    {
        if (result != J2534Error.STATUS_NOERROR)
        {
            var errorMsg = GetLastError();
            Log.Error("J2534 Error in {Function}: {Error} - {Message}", function, result, errorMsg);
            throw new J2534Exception(result, $"{function}: {errorMsg}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        lock (_lock)
        {
            if (_libraryHandle != IntPtr.Zero)
            {
                FreeLibrary(_libraryHandle);
                _libraryHandle = IntPtr.Zero;
            }
            _disposed = true;
        }
    }
}
