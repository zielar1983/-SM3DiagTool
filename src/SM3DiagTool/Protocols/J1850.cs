using SM3DiagTool.J2534;
using SM3DiagTool.Models;
using Serilog;

namespace SM3DiagTool.Protocols;

public class J1850
{
    private readonly J2534Channel _channel;
    private readonly bool _isVpw;

    public J1850(J2534Channel channel, bool isVpw = true)
    {
        _channel = channel;
        _isVpw = isVpw;
    }

    public static J2534Channel OpenVpwChannel(J2534Device device)
    {
        var channel = device.OpenChannel(J2534Protocol.J1850VPW, J2534ConnectFlag.None, 10400);

        var mask = new byte[] { 0x00 };
        var pattern = new byte[] { 0x00 };
        channel.SetPassFilter(mask, pattern);

        Log.Information("J1850 VPW channel opened at 10400 baud");
        return channel;
    }

    public static J2534Channel OpenPwmChannel(J2534Device device)
    {
        var channel = device.OpenChannel(J2534Protocol.J1850PWM, J2534ConnectFlag.None, 41600);

        var mask = new byte[] { 0x00, 0x00, 0x00 };
        var pattern = new byte[] { 0x00, 0x00, 0x00 };
        channel.SetPassFilter(mask, pattern);

        Log.Information("J1850 PWM channel opened at 41600 baud");
        return channel;
    }

    public byte[] SendRequest(byte[] header, byte serviceId, byte[]? data = null)
    {
        var request = new List<byte>();
        request.AddRange(header);
        request.Add(serviceId);
        if (data != null) request.AddRange(data);

        _channel.ClearRxBuffer();
        _channel.WriteMsg(request.ToArray(), J2534TxFlag.None);

        var responses = _channel.ReadMsgs(1, 3000);
        foreach (var resp in responses)
        {
            var respData = resp.GetData();
            if (respData.Length > header.Length)
                return respData.Skip(header.Length).ToArray();
        }
        return Array.Empty<byte>();
    }

    public List<DiagnosticTroubleCode> ReadDtcs()
    {
        var dtcs = new List<DiagnosticTroubleCode>();
        var header = _isVpw
            ? new byte[] { 0x68, 0x6A, 0xF1 }  // VPW functional request
            : new byte[] { 0x61, 0x6A, 0xF1 };  // PWM functional request

        var response = SendRequest(header, 0x03); // Mode 03 - Read DTCs

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
        var header = _isVpw
            ? new byte[] { 0x68, 0x6A, 0xF1 }
            : new byte[] { 0x61, 0x6A, 0xF1 };

        var response = SendRequest(header, 0x04); // Mode 04 - Clear DTCs
        return response.Length > 0 && response[0] == 0x44;
    }

    public byte[] ReadPid(byte pid)
    {
        var header = _isVpw
            ? new byte[] { 0x68, 0x6A, 0xF1 }
            : new byte[] { 0x61, 0x6A, 0xF1 };

        return SendRequest(header, 0x01, new[] { pid }); // Mode 01 - Live data
    }

    public string ReadVin()
    {
        var header = _isVpw
            ? new byte[] { 0x68, 0x6A, 0xF1 }
            : new byte[] { 0x61, 0x6A, 0xF1 };

        var response = SendRequest(header, 0x09, new byte[] { 0x02 }); // Mode 09, PID 02
        if (response.Length > 2)
            return new string(response.Skip(2).Select(b => b >= 0x20 && b <= 0x7E ? (char)b : ' ').ToArray()).Trim();
        return "";
    }

    public List<CanMessage> ReadRawMessages(int count = 50, uint timeout = 100)
    {
        var messages = new List<CanMessage>();
        var responses = _channel.ReadMsgs((uint)count, timeout);

        foreach (var resp in responses)
        {
            var data = resp.GetData();
            if (data.Length >= 3)
            {
                messages.Add(new CanMessage
                {
                    Id = (uint)((data[0] << 16) | (data[1] << 8) | data[2]),
                    Data = data.Skip(3).ToArray(),
                    IsTx = (resp.RxStatus & J2534RxStatus.TX_MSG_TYPE) != 0,
                    Timestamp = DateTime.Now
                });
            }
        }

        return messages;
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
