using Serilog;

namespace SM3DiagTool.J2534;

public sealed class J2534Channel : IDisposable
{
    private readonly J2534Api _api;
    private readonly J2534Device _device;
    private readonly List<uint> _filterIds = new();
    private readonly List<uint> _periodicMsgIds = new();
    private bool _disposed;

    public uint ChannelId { get; }
    public J2534Protocol Protocol { get; }
    public uint BaudRate { get; }

    internal J2534Channel(J2534Api api, J2534Device device, uint channelId,
        J2534Protocol protocol, uint baudRate)
    {
        _api = api;
        _device = device;
        ChannelId = channelId;
        Protocol = protocol;
        BaudRate = baudRate;
    }

    public PassThruMsg[] ReadMsgs(uint count = 1, uint timeout = 1000)
    {
        return _api.ReadMsgs(ChannelId, count, timeout);
    }

    public void WriteMsgs(params PassThruMsg[] msgs)
    {
        _api.WriteMsgs(ChannelId, msgs, 1000);
    }

    public void WriteMsg(byte[] data, J2534TxFlag flags = J2534TxFlag.None)
    {
        var msg = new PassThruMsg(Protocol);
        msg.SetData(data);
        msg.TxFlags = (uint)flags;
        WriteMsgs(msg);
    }

    public uint SetFlowControlFilter(byte[] maskId, byte[] patternId, byte[] flowControlId)
    {
        var mask = new PassThruMsg(Protocol);
        mask.SetData(maskId);

        var pattern = new PassThruMsg(Protocol);
        pattern.SetData(patternId);

        var flowControl = new PassThruMsg(Protocol);
        flowControl.SetData(flowControlId);

        var filterId = _api.StartMsgFilter(ChannelId, J2534FilterType.FLOW_CONTROL, mask, pattern, flowControl);
        _filterIds.Add(filterId);
        return filterId;
    }

    public uint SetPassFilter(byte[] maskData, byte[] patternData)
    {
        var mask = new PassThruMsg(Protocol);
        mask.SetData(maskData);

        var pattern = new PassThruMsg(Protocol);
        pattern.SetData(patternData);

        var filterId = _api.StartMsgFilter(ChannelId, J2534FilterType.PASS_FILTER, mask, pattern, null);
        _filterIds.Add(filterId);
        return filterId;
    }

    public uint SetBlockFilter(byte[] maskData, byte[] patternData)
    {
        var mask = new PassThruMsg(Protocol);
        mask.SetData(maskData);

        var pattern = new PassThruMsg(Protocol);
        pattern.SetData(patternData);

        var filterId = _api.StartMsgFilter(ChannelId, J2534FilterType.BLOCK_FILTER, mask, pattern, null);
        _filterIds.Add(filterId);
        return filterId;
    }

    public void RemoveFilter(uint filterId)
    {
        _api.StopMsgFilter(ChannelId, filterId);
        _filterIds.Remove(filterId);
    }

    public uint StartPeriodicMsg(byte[] data, uint intervalMs, J2534TxFlag flags = J2534TxFlag.None)
    {
        var msg = new PassThruMsg(Protocol);
        msg.SetData(data);
        msg.TxFlags = (uint)flags;
        var msgId = _api.StartPeriodicMsg(ChannelId, msg, intervalMs);
        _periodicMsgIds.Add(msgId);
        return msgId;
    }

    public void StopPeriodicMsg(uint msgId)
    {
        _api.StopPeriodicMsg(ChannelId, msgId);
        _periodicMsgIds.Remove(msgId);
    }

    public void ClearRxBuffer() => _api.ClearRxBuffer(ChannelId);
    public void ClearTxBuffer() => _api.ClearTxBuffer(ChannelId);
    public void ClearFilters() => _api.ClearMsgFilters(ChannelId);

    public void SetConfig(J2534ConfigParameter parameter, uint value)
    {
        _api.SetConfig(ChannelId, parameter, value);
    }

    public uint GetConfig(J2534ConfigParameter parameter)
    {
        return _api.GetConfig(ChannelId, parameter);
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var msgId in _periodicMsgIds.ToList())
        {
            try { _api.StopPeriodicMsg(ChannelId, msgId); }
            catch (Exception ex) { Log.Warning(ex, "Error stopping periodic msg {MsgId}", msgId); }
        }
        _periodicMsgIds.Clear();

        foreach (var filterId in _filterIds.ToList())
        {
            try { _api.StopMsgFilter(ChannelId, filterId); }
            catch (Exception ex) { Log.Warning(ex, "Error removing filter {FilterId}", filterId); }
        }
        _filterIds.Clear();

        try { _api.Disconnect(ChannelId); }
        catch (Exception ex) { Log.Warning(ex, "Error disconnecting channel {ChannelId}", ChannelId); }

        _device.RemoveChannel(this);
        _disposed = true;
    }
}
