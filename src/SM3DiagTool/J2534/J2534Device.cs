using Serilog;

namespace SM3DiagTool.J2534;

public sealed class J2534Device : IDisposable
{
    private readonly J2534Api _api;
    private readonly List<J2534Channel> _channels = new();
    private bool _disposed;

    public uint DeviceId { get; private set; }
    public J2534DeviceInfo DeviceInfo { get; }
    public bool IsOpen { get; private set; }

    public J2534Device(J2534Api api, J2534DeviceInfo deviceInfo)
    {
        _api = api;
        DeviceInfo = deviceInfo;
    }

    public void Open()
    {
        if (IsOpen) return;
        if (!_api.IsLoaded)
            _api.LoadLibrary(DeviceInfo.DllPath);

        DeviceId = _api.Open();
        IsOpen = true;
        Log.Information("J2534 Device opened: {Device}", DeviceInfo.Name);
    }

    public J2534Channel OpenChannel(J2534Protocol protocol, J2534ConnectFlag flags, uint baudRate)
    {
        if (!IsOpen)
            throw new J2534Exception("Device not open");

        var channelId = _api.Connect(DeviceId, protocol, flags, baudRate);
        var channel = new J2534Channel(_api, this, channelId, protocol, baudRate);
        _channels.Add(channel);
        return channel;
    }

    public (string firmware, string dll, string api) ReadVersion()
    {
        if (!IsOpen) throw new J2534Exception("Device not open");
        return _api.ReadVersion(DeviceId);
    }

    public uint ReadBatteryVoltage()
    {
        if (!IsOpen) throw new J2534Exception("Device not open");
        return _api.ReadBatteryVoltage(DeviceId);
    }

    public void SetProgrammingVoltage(uint pinNumber, uint voltage)
    {
        if (!IsOpen) throw new J2534Exception("Device not open");
        _api.SetProgrammingVoltage(DeviceId, pinNumber, voltage);
    }

    internal void RemoveChannel(J2534Channel channel)
    {
        _channels.Remove(channel);
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var channel in _channels.ToList())
            channel.Dispose();
        _channels.Clear();

        if (IsOpen)
        {
            try { _api.Close(DeviceId); }
            catch (Exception ex) { Log.Warning(ex, "Error closing J2534 device"); }
            IsOpen = false;
        }
        _disposed = true;
    }
}
