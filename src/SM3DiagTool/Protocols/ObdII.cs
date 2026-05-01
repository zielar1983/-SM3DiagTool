using SM3DiagTool.J2534;
using SM3DiagTool.Models;
using Serilog;

namespace SM3DiagTool.Protocols;

public class ObdII
{
    private readonly J2534Channel _channel;

    private const uint OBD_REQUEST_ID = 0x7DF;
    private const uint OBD_RESPONSE_BASE = 0x7E8;

    public ObdII(J2534Channel channel)
    {
        _channel = channel;
    }

    public static J2534Channel OpenChannel(J2534Device device, uint baudRate = 500000)
    {
        var channel = device.OpenChannel(J2534Protocol.ISO15765, J2534ConnectFlag.None, baudRate);

        var mask = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var pattern = new byte[] { 0x00, 0x00, 0x07, 0xE8 };
        var flowControl = new byte[] { 0x00, 0x00, 0x07, 0xDF };
        channel.SetFlowControlFilter(mask, pattern, flowControl);

        for (uint i = 1; i <= 7; i++)
        {
            var respPattern = new byte[] { 0x00, 0x00, (byte)((0x7E8 + i) >> 8), (byte)(0x7E8 + i) };
            var fcId = new byte[] { 0x00, 0x00, (byte)((0x7E0 + i) >> 8), (byte)(0x7E0 + i) };
            try { channel.SetFlowControlFilter(mask, respPattern, fcId); }
            catch { /* not all ECUs respond */ }
        }

        return channel;
    }

    public byte[] SendRequest(byte serviceId, byte[]? data = null)
    {
        var request = new List<byte> { 0x00, 0x00, 0x07, 0xDF, serviceId };
        if (data != null)
            request.AddRange(data);

        _channel.ClearRxBuffer();
        _channel.WriteMsg(request.ToArray(), J2534TxFlag.ISO15765_FRAME_PAD);

        var responses = _channel.ReadMsgs(16, 2000);
        foreach (var resp in responses)
        {
            var respData = resp.GetData();
            if (respData.Length > 4 && respData[4] == serviceId + 0x40)
                return respData.Skip(4).ToArray();
        }

        return Array.Empty<byte>();
    }

    public List<byte> GetSupportedPids(byte rangeStart)
    {
        var supported = new List<byte>();
        var response = SendRequest(0x01, new[] { rangeStart });

        if (response.Length < 6) return supported;

        uint bitmap = (uint)((response[2] << 24) | (response[3] << 16) | (response[4] << 8) | response[5]);
        for (int i = 0; i < 32; i++)
        {
            if ((bitmap & (1u << (31 - i))) != 0)
                supported.Add((byte)(rangeStart + i + 1));
        }

        return supported;
    }

    public List<byte> GetAllSupportedPids()
    {
        var allPids = new List<byte>();
        byte[] ranges = { 0x00, 0x20, 0x40, 0x60, 0x80, 0xA0, 0xC0 };

        foreach (var range in ranges)
        {
            var pids = GetSupportedPids(range);
            allPids.AddRange(pids.Where(p => p % 0x20 != 0));

            if (!pids.Contains((byte)(range + 0x20)))
                break;
        }

        return allPids;
    }

    public PidData? ReadPid(byte pid)
    {
        var response = SendRequest(0x01, new[] { pid });
        if (response.Length < 3) return null;

        if (!ObdPids.StandardPids.TryGetValue(pid, out var pidDef))
        {
            return new PidData
            {
                Pid = pid,
                Name = $"PID 0x{pid:X2}",
                Unit = "",
                Value = response.Length > 2 ? response[2] : 0,
                Timestamp = DateTime.Now
            };
        }

        var clone = new PidData
        {
            Pid = pidDef.Pid,
            Name = pidDef.Name,
            Unit = pidDef.Unit,
            MinValue = pidDef.MinValue,
            MaxValue = pidDef.MaxValue,
            Parser = pidDef.Parser,
            Timestamp = DateTime.Now
        };

        if (clone.Parser != null)
            clone.Value = clone.Parser(response.Skip(2).ToArray());

        return clone;
    }

    public Dictionary<byte, PidData> ReadMultiplePids(IEnumerable<byte> pids)
    {
        var results = new Dictionary<byte, PidData>();
        foreach (var pid in pids)
        {
            try
            {
                var data = ReadPid(pid);
                if (data != null) results[pid] = data;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to read PID 0x{Pid:X2}", pid);
            }
        }
        return results;
    }

    public List<DiagnosticTroubleCode> ReadDtcs()
    {
        var dtcs = new List<DiagnosticTroubleCode>();

        var storedResponse = SendRequest(0x03);
        if (storedResponse.Length > 1)
            dtcs.AddRange(ParseDtcs(storedResponse.Skip(1).ToArray(), DtcStatus.Stored));

        var pendingResponse = SendRequest(0x07);
        if (pendingResponse.Length > 1)
            dtcs.AddRange(ParseDtcs(pendingResponse.Skip(1).ToArray(), DtcStatus.Pending));

        var permanentResponse = SendRequest(0x0A);
        if (permanentResponse.Length > 1)
            dtcs.AddRange(ParseDtcs(permanentResponse.Skip(1).ToArray(), DtcStatus.Permanent));

        return dtcs;
    }

    public bool ClearDtcs()
    {
        try
        {
            var request = new byte[] { 0x00, 0x00, 0x07, 0xDF, 0x04 };
            _channel.ClearRxBuffer();
            _channel.WriteMsg(request, J2534TxFlag.ISO15765_FRAME_PAD);
            var responses = _channel.ReadMsgs(1, 5000);
            return responses.Any(r => r.GetData().Length > 4 && r.GetData()[4] == 0x44);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to clear DTCs");
            return false;
        }
    }

    public string ReadVin()
    {
        var response = SendRequest(0x09, new byte[] { 0x02 });
        if (response.Length >= 19)
        {
            return new string(response.Skip(2).Take(17)
                .Select(b => (char)b).ToArray()).Trim();
        }
        return string.Empty;
    }

    public string ReadCalibrationId()
    {
        var response = SendRequest(0x09, new byte[] { 0x04 });
        if (response.Length > 2)
        {
            return new string(response.Skip(2)
                .TakeWhile(b => b != 0)
                .Select(b => (char)b).ToArray()).Trim();
        }
        return string.Empty;
    }

    private static List<DiagnosticTroubleCode> ParseDtcs(byte[] data, DtcStatus status)
    {
        var dtcs = new List<DiagnosticTroubleCode>();
        for (int i = 0; i + 1 < data.Length; i += 2)
        {
            if (data[i] == 0 && data[i + 1] == 0) continue;

            var code = DecodeDtc(data[i], data[i + 1]);
            dtcs.Add(new DiagnosticTroubleCode
            {
                Code = code,
                Description = GetDtcDescription(code),
                Status = status,
                Module = "ECM"
            });
        }
        return dtcs;
    }

    private static string DecodeDtc(byte high, byte low)
    {
        char prefix = ((high >> 6) & 0x03) switch
        {
            0 => 'P',
            1 => 'C',
            2 => 'B',
            3 => 'U',
            _ => 'P'
        };

        int digit1 = (high >> 4) & 0x03;
        int digit2 = high & 0x0F;
        int digit3 = (low >> 4) & 0x0F;
        int digit4 = low & 0x0F;

        return $"{prefix}{digit1}{digit2:X}{digit3:X}{digit4:X}";
    }

    private static string GetDtcDescription(string code)
    {
        return KnownDtcCodes.Descriptions.GetValueOrDefault(code, "Nieznany kod bledu");
    }
}
