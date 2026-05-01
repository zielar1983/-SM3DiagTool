using SM3DiagTool.J2534;
using SM3DiagTool.Models;
using Serilog;

namespace SM3DiagTool.Protocols;

public class CanBus
{
    private readonly J2534Channel _channel;

    public CanBus(J2534Channel channel)
    {
        _channel = channel;
    }

    public static J2534Channel OpenChannel(J2534Device device, uint baudRate = 500000, bool extendedId = false)
    {
        var flags = extendedId ? J2534ConnectFlag.CAN_29BIT_ID : J2534ConnectFlag.None;
        var channel = device.OpenChannel(J2534Protocol.CAN, flags, baudRate);

        var mask = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var pattern = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        channel.SetPassFilter(mask, pattern);

        return channel;
    }

    public static J2534Channel OpenCanFdChannel(J2534Device device, uint baudRate = 500000,
        J2534ConnectFlag dataPhaseRate = J2534ConnectFlag.CAN_FD_DATA_PHASE_2M)
    {
        var flags = dataPhaseRate;
        var channel = device.OpenChannel(J2534Protocol.CAN_FD, flags, baudRate);

        var mask = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var pattern = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        channel.SetPassFilter(mask, pattern);

        return channel;
    }

    public void SendFrame(uint canId, byte[] data, bool extendedId = false)
    {
        var msgData = new byte[4 + data.Length];
        msgData[0] = (byte)(canId >> 24);
        msgData[1] = (byte)(canId >> 16);
        msgData[2] = (byte)(canId >> 8);
        msgData[3] = (byte)canId;
        Array.Copy(data, 0, msgData, 4, data.Length);

        var flags = extendedId ? J2534TxFlag.CAN_29BIT_ID : J2534TxFlag.None;
        _channel.WriteMsg(msgData, flags);
    }

    public List<CanMessage> ReadFrames(uint count = 100, uint timeout = 100)
    {
        var messages = new List<CanMessage>();
        var rawMsgs = _channel.ReadMsgs(count, timeout);

        foreach (var raw in rawMsgs)
        {
            var data = raw.GetData();
            if (data.Length < 4) continue;

            uint canId = (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]);
            var canData = data.Skip(4).ToArray();

            messages.Add(new CanMessage
            {
                Id = canId,
                Data = canData,
                IsExtendedId = ((J2534RxStatus)raw.RxStatus & J2534RxStatus.CAN_29BIT_ID) != 0,
                IsTx = ((J2534RxStatus)raw.RxStatus & J2534RxStatus.TX_MSG_TYPE) != 0,
                Timestamp = DateTime.Now
            });
        }

        return messages;
    }
}
