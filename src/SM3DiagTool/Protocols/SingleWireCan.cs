using SM3DiagTool.J2534;
using SM3DiagTool.Models;
using Serilog;

namespace SM3DiagTool.Protocols;

public class SingleWireCan
{
    private readonly J2534Channel _channel;

    public SingleWireCan(J2534Channel channel)
    {
        _channel = channel;
    }

    public static J2534Channel OpenChannel(J2534Device device, uint baudRate = 33333)
    {
        var channel = device.OpenChannel(J2534Protocol.CAN, J2534ConnectFlag.None, baudRate);

        var mask = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var pattern = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        channel.SetPassFilter(mask, pattern);

        Log.Information("Single Wire CAN (GMLAN) channel opened at {BaudRate} baud", baudRate);
        return channel;
    }

    public static J2534Channel OpenHighSpeedChannel(J2534Device device, uint baudRate = 500000)
    {
        var channel = device.OpenChannel(J2534Protocol.CAN, J2534ConnectFlag.None, baudRate);

        var mask = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var pattern = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        channel.SetPassFilter(mask, pattern);

        Log.Information("GMLAN high-speed CAN channel opened at {BaudRate} baud", baudRate);
        return channel;
    }

    public void SendFrame(uint canId, byte[] data)
    {
        var msg = new List<byte>
        {
            (byte)(canId >> 24), (byte)(canId >> 16), (byte)(canId >> 8), (byte)canId
        };
        msg.AddRange(data);

        _channel.WriteMsg(msg.ToArray(), J2534TxFlag.None);
    }

    public List<CanMessage> ReadFrames(int count = 50, uint timeout = 100)
    {
        var messages = new List<CanMessage>();
        var responses = _channel.ReadMsgs((uint)count, timeout);

        foreach (var resp in responses)
        {
            var data = resp.GetData();
            if (data.Length >= 4)
            {
                messages.Add(new CanMessage
                {
                    Id = (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]),
                    Data = data.Skip(4).ToArray(),
                    IsTx = (resp.RxStatus & J2534RxStatus.TX_MSG_TYPE) != 0,
                    Timestamp = DateTime.Now
                });
            }
        }

        return messages;
    }

    public byte[] SendDiagnosticRequest(uint txId, uint rxId, byte serviceId, byte[]? data = null)
    {
        var request = new List<byte>
        {
            (byte)(txId >> 24), (byte)(txId >> 16), (byte)(txId >> 8), (byte)txId,
            serviceId
        };
        if (data != null) request.AddRange(data);

        _channel.ClearRxBuffer();
        _channel.WriteMsg(request.ToArray(), J2534TxFlag.None);

        var responses = _channel.ReadMsgs(1, 3000);
        foreach (var resp in responses)
        {
            var respData = resp.GetData();
            if (respData.Length > 4)
            {
                uint respId = (uint)((respData[0] << 24) | (respData[1] << 16) | (respData[2] << 8) | respData[3]);
                if (respId == rxId)
                    return respData.Skip(4).ToArray();
            }
        }

        return Array.Empty<byte>();
    }

    public void DisableNormalCommunication()
    {
        SendFrame(0x101, new byte[] { 0x01, 0x28, 0x03, 0x01, 0x00, 0x00, 0x00, 0x00 });
        Log.Information("GMLAN: Disabled normal communication");
    }

    public void EnableNormalCommunication()
    {
        SendFrame(0x101, new byte[] { 0x01, 0x28, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 });
        Log.Information("GMLAN: Enabled normal communication");
    }
}
