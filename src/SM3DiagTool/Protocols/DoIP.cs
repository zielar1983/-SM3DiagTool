using SM3DiagTool.J2534;
using SM3DiagTool.Models;
using Serilog;

namespace SM3DiagTool.Protocols;

public class DoIP
{
    private readonly J2534Channel _channel;
    private readonly ushort _sourceAddress;
    private readonly ushort _targetAddress;

    public static class PayloadType
    {
        public const ushort VehicleIdentificationRequest = 0x0001;
        public const ushort VehicleIdentificationRequestWithEid = 0x0002;
        public const ushort VehicleIdentificationRequestWithVin = 0x0003;
        public const ushort VehicleAnnouncementResponse = 0x0004;
        public const ushort RoutingActivationRequest = 0x0005;
        public const ushort RoutingActivationResponse = 0x0006;
        public const ushort DiagnosticMessage = 0x8001;
        public const ushort DiagnosticMessagePositiveAck = 0x8002;
        public const ushort DiagnosticMessageNegativeAck = 0x8003;
        public const ushort AliveCheckRequest = 0x0007;
        public const ushort AliveCheckResponse = 0x0008;
        public const ushort EntityStatusRequest = 0x4001;
        public const ushort EntityStatusResponse = 0x4002;
        public const ushort PowerModeRequest = 0x4003;
        public const ushort PowerModeResponse = 0x4004;
    }

    public static class RoutingActivationType
    {
        public const byte Default = 0x00;
        public const byte DiagnosticRequired = 0x01;
    }

    public DoIP(J2534Channel channel, ushort sourceAddress = 0x0E00, ushort targetAddress = 0x0001)
    {
        _channel = channel;
        _sourceAddress = sourceAddress;
        _targetAddress = targetAddress;
    }

    public static J2534Channel OpenChannel(J2534Device device, uint baudRate = 0)
    {
        var channel = device.OpenChannel(J2534Protocol.DoIP, J2534ConnectFlag.None, baudRate);
        return channel;
    }

    public byte[] SendDiagnosticRequest(byte[] data, int timeout = 5000)
    {
        var message = new List<byte>();
        // DoIP header: protocol version (0x02), inverse (0xFD), payload type, length
        message.Add(0x02); // protocol version
        message.Add(0xFD); // inverse version
        message.Add((byte)(PayloadType.DiagnosticMessage >> 8));
        message.Add((byte)PayloadType.DiagnosticMessage);

        uint payloadLength = (uint)(4 + data.Length); // SA(2) + TA(2) + data
        message.Add((byte)(payloadLength >> 24));
        message.Add((byte)(payloadLength >> 16));
        message.Add((byte)(payloadLength >> 8));
        message.Add((byte)payloadLength);

        // Source and target address
        message.Add((byte)(_sourceAddress >> 8));
        message.Add((byte)_sourceAddress);
        message.Add((byte)(_targetAddress >> 8));
        message.Add((byte)_targetAddress);

        message.AddRange(data);

        _channel.ClearRxBuffer();
        _channel.WriteMsg(message.ToArray(), J2534TxFlag.None);

        var responses = _channel.ReadMsgs(1, (uint)timeout);
        foreach (var resp in responses)
        {
            var respData = resp.GetData();
            if (respData.Length > 12)
            {
                ushort payloadType = (ushort)((respData[2] << 8) | respData[3]);
                if (payloadType == PayloadType.DiagnosticMessage)
                    return respData.Skip(12).ToArray();
            }
        }

        return Array.Empty<byte>();
    }

    public bool ActivateRouting(byte activationType = RoutingActivationType.Default)
    {
        var message = new List<byte>
        {
            0x02, 0xFD,
            (byte)(PayloadType.RoutingActivationRequest >> 8),
            (byte)PayloadType.RoutingActivationRequest,
            0x00, 0x00, 0x00, 0x07,
            (byte)(_sourceAddress >> 8), (byte)_sourceAddress,
            activationType,
            0x00, 0x00, 0x00, 0x00
        };

        _channel.ClearRxBuffer();
        _channel.WriteMsg(message.ToArray(), J2534TxFlag.None);

        var responses = _channel.ReadMsgs(1, 5000);
        foreach (var resp in responses)
        {
            var data = resp.GetData();
            if (data.Length >= 8)
            {
                ushort payloadType = (ushort)((data[2] << 8) | data[3]);
                if (payloadType == PayloadType.RoutingActivationResponse)
                {
                    Log.Information("DoIP routing activated: SA=0x{Source:X4}, TA=0x{Target:X4}", _sourceAddress, _targetAddress);
                    return true;
                }
            }
        }

        return false;
    }

    public List<DoIPEntity> DiscoverEntities(int timeout = 3000)
    {
        var entities = new List<DoIPEntity>();

        var message = new byte[]
        {
            0x02, 0xFD,
            (byte)(PayloadType.VehicleIdentificationRequest >> 8),
            (byte)PayloadType.VehicleIdentificationRequest,
            0x00, 0x00, 0x00, 0x00
        };

        _channel.ClearRxBuffer();
        _channel.WriteMsg(message, J2534TxFlag.None);

        try
        {
            var responses = _channel.ReadMsgs(10, (uint)timeout);
            foreach (var resp in responses)
            {
                var data = resp.GetData();
                if (data.Length >= 41)
                {
                    ushort payloadType = (ushort)((data[2] << 8) | data[3]);
                    if (payloadType == PayloadType.VehicleAnnouncementResponse)
                    {
                        var entity = new DoIPEntity
                        {
                            Vin = new string(data.Skip(8).Take(17).Select(b => (char)b).ToArray()),
                            LogicalAddress = (ushort)((data[25] << 8) | data[26]),
                            Eid = data.Skip(27).Take(6).ToArray(),
                            Gid = data.Skip(33).Take(6).ToArray(),
                            FurtherActionRequired = data.Length > 39 ? data[39] : (byte)0,
                            SyncStatus = data.Length > 40 ? data[40] : (byte)0
                        };
                        entities.Add(entity);
                        Log.Information("DoIP entity found: VIN={Vin}, Address=0x{Address:X4}", entity.Vin, entity.LogicalAddress);
                    }
                }
            }
        }
        catch (J2534Exception) { }

        return entities;
    }

    public byte[] SendUdsRequest(byte serviceId, byte[]? subData = null)
    {
        var udsData = new List<byte> { serviceId };
        if (subData != null) udsData.AddRange(subData);

        var response = SendDiagnosticRequest(udsData.ToArray());
        if (response.Length > 0 && response[0] == serviceId + 0x40)
            return response;

        if (response.Length > 2 && response[0] == 0x7F)
        {
            byte nrc = response[2];
            if (nrc == 0x78) // response pending
            {
                var pendingResp = SendDiagnosticRequest(Array.Empty<byte>(), 15000);
                if (pendingResp.Length > 0 && pendingResp[0] == serviceId + 0x40)
                    return pendingResp;
            }
            throw new J2534Exception($"DoIP UDS NRC: 0x{nrc:X2}");
        }

        return response;
    }

    public byte[] GetEntityStatus()
    {
        var message = new byte[]
        {
            0x02, 0xFD,
            (byte)(PayloadType.EntityStatusRequest >> 8),
            (byte)PayloadType.EntityStatusRequest,
            0x00, 0x00, 0x00, 0x00
        };

        _channel.WriteMsg(message, J2534TxFlag.None);
        var responses = _channel.ReadMsgs(1, 3000);
        foreach (var resp in responses)
        {
            var data = resp.GetData();
            if (data.Length > 8)
                return data.Skip(8).ToArray();
        }
        return Array.Empty<byte>();
    }

    public byte GetPowerMode()
    {
        var message = new byte[]
        {
            0x02, 0xFD,
            (byte)(PayloadType.PowerModeRequest >> 8),
            (byte)PayloadType.PowerModeRequest,
            0x00, 0x00, 0x00, 0x00
        };

        _channel.WriteMsg(message, J2534TxFlag.None);
        var responses = _channel.ReadMsgs(1, 3000);
        foreach (var resp in responses)
        {
            var data = resp.GetData();
            if (data.Length > 8)
                return data[8];
        }
        return 0xFF;
    }
}

public class DoIPEntity
{
    public string Vin { get; set; } = "";
    public ushort LogicalAddress { get; set; }
    public byte[] Eid { get; set; } = Array.Empty<byte>();
    public byte[] Gid { get; set; } = Array.Empty<byte>();
    public byte FurtherActionRequired { get; set; }
    public byte SyncStatus { get; set; }

    public string EidHex => BitConverter.ToString(Eid).Replace("-", ":");
    public string GidHex => BitConverter.ToString(Gid).Replace("-", ":");
    public string AddressHex => $"0x{LogicalAddress:X4}";
}
