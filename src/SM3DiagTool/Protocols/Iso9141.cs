using SM3DiagTool.J2534;
using SM3DiagTool.Models;
using Serilog;

namespace SM3DiagTool.Protocols;

public class Iso9141
{
    private readonly J2534Channel _channel;

    public Iso9141(J2534Channel channel)
    {
        _channel = channel;
    }

    public static J2534Channel OpenChannel(J2534Device device, uint baudRate = 10400)
    {
        var channel = device.OpenChannel(J2534Protocol.ISO9141, J2534ConnectFlag.None, baudRate);

        var mask = new byte[] { 0x00, 0x00, 0x00 };
        var pattern = new byte[] { 0x00, 0x00, 0x00 };
        channel.SetPassFilter(mask, pattern);

        Log.Information("ISO 9141 channel opened at {BaudRate} baud", baudRate);
        return channel;
    }

    public static J2534Channel OpenChannelWithInit(J2534Device device, byte targetAddress = 0x33)
    {
        var channel = device.OpenChannel(J2534Protocol.ISO9141, J2534ConnectFlag.None, 10400);

        // 5-baud init
        var initMsg = new byte[] { targetAddress };
        channel.Ioctl(J2534Ioctl.FIVE_BAUD_INIT, initMsg);

        var mask = new byte[] { 0x00, 0x00, 0x00 };
        var pattern = new byte[] { 0x00, 0x00, 0x00 };
        channel.SetPassFilter(mask, pattern);

        Log.Information("ISO 9141 channel opened with 5-baud init, target=0x{Target:X2}", targetAddress);
        return channel;
    }

    public byte[] SendRequest(byte serviceId, byte[]? data = null)
    {
        var request = new List<byte>
        {
            0x68, // header: priority/type
            0x6A, // target address (broadcast)
            0xF1, // source address (tester)
            serviceId
        };
        if (data != null) request.AddRange(data);

        // Add checksum
        byte checksum = 0;
        foreach (var b in request) checksum += b;
        request.Add(checksum);

        _channel.ClearRxBuffer();
        _channel.WriteMsg(request.ToArray(), J2534TxFlag.None);

        var responses = _channel.ReadMsgs(1, 5000);
        foreach (var resp in responses)
        {
            var respData = resp.GetData();
            if (respData.Length > 4)
                return respData.Skip(3).Take(respData.Length - 4).ToArray(); // skip header, remove checksum
        }

        return Array.Empty<byte>();
    }

    public List<DiagnosticTroubleCode> ReadDtcs()
    {
        var dtcs = new List<DiagnosticTroubleCode>();
        var response = SendRequest(0x03);

        if (response.Length > 1)
        {
            for (int i = 1; i + 1 < response.Length; i += 2)
            {
                var code = DecodeDtc(response[i], response[i + 1]);
                if (code != "P0000")
                {
                    dtcs.Add(new DiagnosticTroubleCode
                    {
                        Code = code,
                        Description = KnownDtcCodes.Descriptions.GetValueOrDefault(code, "Nieznany kod bledu"),
                        Status = DtcStatus.Active,
                        Module = "ECM"
                    });
                }
            }
        }
        return dtcs;
    }

    public bool ClearDtcs()
    {
        var response = SendRequest(0x04);
        return response.Length > 0 && response[0] == 0x44;
    }

    public byte[] ReadPid(byte pid)
    {
        return SendRequest(0x01, new[] { pid });
    }

    public string ReadVin()
    {
        var response = SendRequest(0x09, new byte[] { 0x02 });
        if (response.Length > 2)
            return new string(response.Skip(2).Select(b => b >= 0x20 && b <= 0x7E ? (char)b : ' ').ToArray()).Trim();
        return "";
    }

    public byte[] ReadFreezeFrameData(byte frameNumber = 0x00)
    {
        return SendRequest(0x02, new byte[] { 0x00, frameNumber });
    }

    public List<byte> GetSupportedPids()
    {
        var supported = new List<byte>();
        byte[] pidRanges = { 0x00, 0x20, 0x40, 0x60, 0x80, 0xA0, 0xC0, 0xE0 };

        foreach (byte startPid in pidRanges)
        {
            try
            {
                var response = SendRequest(0x01, new[] { startPid });
                if (response.Length >= 6)
                {
                    uint bits = (uint)((response[2] << 24) | (response[3] << 16) | (response[4] << 8) | response[5]);
                    for (int i = 0; i < 32; i++)
                    {
                        if ((bits & (1u << (31 - i))) != 0)
                            supported.Add((byte)(startPid + i + 1));
                    }

                    if ((bits & 1) == 0) break;
                }
                else break;
            }
            catch { break; }
        }

        return supported;
    }

    private static string DecodeDtc(byte high, byte low)
    {
        char prefix = ((high >> 6) & 0x03) switch
        {
            0 => 'P', 1 => 'C', 2 => 'B', 3 => 'U', _ => 'P'
        };
        return $"{prefix}{(high >> 4) & 0x03}{high & 0x0F:X}{low >> 4:X}{low & 0x0F:X}";
    }
}
