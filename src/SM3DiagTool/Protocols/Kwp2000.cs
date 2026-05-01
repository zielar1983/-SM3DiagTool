using SM3DiagTool.J2534;
using SM3DiagTool.Models;
using Serilog;

namespace SM3DiagTool.Protocols;

public class Kwp2000
{
    private readonly J2534Channel _channel;
    private readonly byte _targetAddress;
    private readonly byte _sourceAddress;

    public static class ServiceId
    {
        public const byte StartDiagnosticSession = 0x10;
        public const byte EcuReset = 0x11;
        public const byte ReadDtc = 0x13;
        public const byte ClearDtc = 0x14;
        public const byte ReadDataByLocalId = 0x21;
        public const byte ReadDataByCommonId = 0x22;
        public const byte ReadMemoryByAddress = 0x23;
        public const byte SecurityAccess = 0x27;
        public const byte WriteDataByCommonId = 0x2E;
        public const byte InputOutputControl = 0x30;
        public const byte StartRoutineByLocalId = 0x31;
        public const byte StopRoutineByLocalId = 0x32;
        public const byte RequestDownload = 0x34;
        public const byte RequestUpload = 0x35;
        public const byte TransferData = 0x36;
        public const byte RequestTransferExit = 0x37;
        public const byte TesterPresent = 0x3E;
        public const byte ReadEcuIdentification = 0x1A;
    }

    public Kwp2000(J2534Channel channel, byte targetAddress = 0x01, byte sourceAddress = 0xF1)
    {
        _channel = channel;
        _targetAddress = targetAddress;
        _sourceAddress = sourceAddress;
    }

    public static J2534Channel OpenKLineChannel(J2534Device device, bool fastInit = true)
    {
        var channel = device.OpenChannel(
            J2534Protocol.ISO14230,
            J2534ConnectFlag.None,
            10400);

        var mask = new byte[] { 0xFF, 0xFF, 0xFF };
        var pattern = new byte[] { 0x00, 0x00, 0x00 };
        channel.SetPassFilter(mask, pattern);

        return channel;
    }

    public static J2534Channel OpenCanChannel(J2534Device device, uint txId, uint rxId, uint baudRate = 500000)
    {
        return Uds.OpenChannel(device, txId, rxId, baudRate);
    }

    public byte[] SendRequest(byte serviceId, byte[]? data = null)
    {
        var request = new List<byte>();

        if (_channel.Protocol == J2534Protocol.ISO14230)
        {
            int dataLen = 1 + (data?.Length ?? 0);
            if (dataLen <= 63)
                request.Add((byte)(0x80 | dataLen));
            else
            {
                request.Add(0x80);
                request.Add((byte)dataLen);
            }
            request.Add(_targetAddress);
            request.Add(_sourceAddress);
        }

        request.Add(serviceId);
        if (data != null) request.AddRange(data);

        if (_channel.Protocol == J2534Protocol.ISO14230)
        {
            byte checksum = 0;
            foreach (var b in request) checksum += b;
            request.Add(checksum);
        }

        _channel.ClearRxBuffer();
        _channel.WriteMsg(request.ToArray());

        var responses = _channel.ReadMsgs(1, 5000);
        foreach (var resp in responses)
        {
            var respData = resp.GetData();
            if (respData.Length > 0)
            {
                int startIdx = 0;
                if (_channel.Protocol == J2534Protocol.ISO14230)
                    startIdx = (respData[0] & 0x3F) > 0 ? 3 : 4;

                if (startIdx < respData.Length && respData[startIdx] == serviceId + 0x40)
                    return respData.Skip(startIdx).ToArray();
            }
        }

        return Array.Empty<byte>();
    }

    public void StartDiagnosticSession(byte sessionType = 0x89)
    {
        SendRequest(ServiceId.StartDiagnosticSession, new[] { sessionType });
        Log.Information("KWP2000 session started: 0x{SessionType:X2}", sessionType);
    }

    public EcuInformation ReadEcuIdentification()
    {
        var info = new EcuInformation();

        try
        {
            var resp = SendRequest(ServiceId.ReadEcuIdentification, new byte[] { 0x01 });
            if (resp.Length > 2)
                info.EcuName = new string(resp.Skip(2).Select(b => (char)b).ToArray()).Trim();
        }
        catch { }

        try
        {
            var resp = SendRequest(ServiceId.ReadEcuIdentification, new byte[] { 0x06 });
            if (resp.Length > 2)
                info.Vin = new string(resp.Skip(2).Take(17).Select(b => (char)b).ToArray()).Trim();
        }
        catch { }

        try
        {
            var resp = SendRequest(ServiceId.ReadEcuIdentification, new byte[] { 0x04 });
            if (resp.Length > 2)
                info.HardwareVersion = new string(resp.Skip(2).Select(b => (char)b).ToArray()).Trim();
        }
        catch { }

        try
        {
            var resp = SendRequest(ServiceId.ReadEcuIdentification, new byte[] { 0x05 });
            if (resp.Length > 2)
                info.SoftwareVersion = new string(resp.Skip(2).Select(b => (char)b).ToArray()).Trim();
        }
        catch { }

        return info;
    }

    public List<DiagnosticTroubleCode> ReadDtcs()
    {
        var dtcs = new List<DiagnosticTroubleCode>();
        try
        {
            var response = SendRequest(ServiceId.ReadDtc, new byte[] { 0x02, 0xFF, 0x00 });
            if (response.Length > 2)
            {
                for (int i = 2; i + 2 < response.Length; i += 3)
                {
                    byte high = response[i];
                    byte low = response[i + 1];
                    byte status = response[i + 2];

                    if (high == 0 && low == 0) continue;

                    char prefix = ((high >> 6) & 0x03) switch
                    {
                        0 => 'P', 1 => 'C', 2 => 'B', 3 => 'U', _ => 'P'
                    };
                    var code = $"{prefix}{(high >> 4) & 0x03}{high & 0x0F:X}{low >> 4:X}{low & 0x0F:X}";

                    dtcs.Add(new DiagnosticTroubleCode
                    {
                        Code = code,
                        Description = KnownDtcCodes.Descriptions.GetValueOrDefault(code, "Nieznany kod bledu"),
                        Status = (status & 0x01) != 0 ? DtcStatus.Active : DtcStatus.Stored,
                        Module = "ECU"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read KWP2000 DTCs");
        }

        return dtcs;
    }

    public bool ClearDtcs()
    {
        try
        {
            SendRequest(ServiceId.ClearDtc, new byte[] { 0xFF, 0x00 });
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to clear KWP2000 DTCs");
            return false;
        }
    }

    public void TesterPresent()
    {
        try { SendRequest(ServiceId.TesterPresent, new byte[] { 0x01 }); } catch { }
    }
}
