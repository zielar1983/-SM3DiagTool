using SM3DiagTool.J2534;
using SM3DiagTool.Models;
using Serilog;

namespace SM3DiagTool.Protocols;

public class Uds
{
    private readonly J2534Channel _channel;
    private readonly uint _txId;
    private readonly uint _rxId;

    public static class ServiceId
    {
        public const byte DiagnosticSessionControl = 0x10;
        public const byte EcuReset = 0x11;
        public const byte ClearDiagnosticInformation = 0x14;
        public const byte ReadDtcInformation = 0x19;
        public const byte ReadDataByIdentifier = 0x22;
        public const byte ReadMemoryByAddress = 0x23;
        public const byte SecurityAccess = 0x27;
        public const byte CommunicationControl = 0x28;
        public const byte WriteDataByIdentifier = 0x2E;
        public const byte InputOutputControlByIdentifier = 0x2F;
        public const byte RoutineControl = 0x31;
        public const byte RequestDownload = 0x34;
        public const byte RequestUpload = 0x35;
        public const byte TransferData = 0x36;
        public const byte RequestTransferExit = 0x37;
        public const byte TesterPresent = 0x3E;
        public const byte ControlDtcSetting = 0x85;
    }

    public static class SessionType
    {
        public const byte Default = 0x01;
        public const byte Programming = 0x02;
        public const byte ExtendedDiagnostic = 0x03;
    }

    public static class DataIdentifier
    {
        public const ushort VIN = 0xF190;
        public const ushort ECUManufacturerName = 0xF154;
        public const ushort ECUSerialNumber = 0xF18C;
        public const ushort VehicleManufacturerPartNumber = 0xF187;
        public const ushort SystemSupplierECUSoftwareNumber = 0xF188;
        public const ushort SystemSupplierECUHardwareNumber = 0xF191;
        public const ushort SystemName = 0xF197;
        public const ushort RepairShopCodeOrTesterSerialNumber = 0xF198;
        public const ushort ProgrammingDate = 0xF199;
        public const ushort CalibrationId = 0xF1A0;
        public const ushort ECUSoftwareVersion = 0xF1A2;
        public const ushort ECUHardwareVersion = 0xF1A3;
        public const ushort BootSoftwareId = 0xF180;
        public const ushort ApplicationSoftwareId = 0xF181;
        public const ushort ActiveDiagSession = 0xF186;
    }

    public Uds(J2534Channel channel, uint txId = 0x7E0, uint rxId = 0x7E8)
    {
        _channel = channel;
        _txId = txId;
        _rxId = rxId;
    }

    public static J2534Channel OpenChannel(J2534Device device, uint txId, uint rxId, uint baudRate = 500000)
    {
        var channel = device.OpenChannel(J2534Protocol.ISO15765, J2534ConnectFlag.None, baudRate);

        var mask = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var pattern = new byte[] {
            (byte)(rxId >> 24), (byte)(rxId >> 16), (byte)(rxId >> 8), (byte)rxId
        };
        var flowControl = new byte[] {
            (byte)(txId >> 24), (byte)(txId >> 16), (byte)(txId >> 8), (byte)txId
        };
        channel.SetFlowControlFilter(mask, pattern, flowControl);

        return channel;
    }

    public byte[] SendRequest(byte serviceId, byte[]? subFunction = null)
    {
        var request = new List<byte>
        {
            (byte)(_txId >> 24), (byte)(_txId >> 16), (byte)(_txId >> 8), (byte)_txId,
            serviceId
        };
        if (subFunction != null)
            request.AddRange(subFunction);

        _channel.ClearRxBuffer();
        _channel.WriteMsg(request.ToArray(), J2534TxFlag.ISO15765_FRAME_PAD);

        var responses = _channel.ReadMsgs(1, 5000);
        foreach (var resp in responses)
        {
            var data = resp.GetData();
            if (data.Length > 4)
            {
                if (data[4] == serviceId + 0x40)
                    return data.Skip(4).ToArray();

                if (data[4] == 0x7F && data.Length > 6)
                {
                    var nrc = data[6];
                    if (nrc == 0x78) // RequestCorrectlyReceivedResponsePending
                    {
                        var pending = _channel.ReadMsgs(1, 10000);
                        foreach (var pResp in pending)
                        {
                            var pData = pResp.GetData();
                            if (pData.Length > 4 && pData[4] == serviceId + 0x40)
                                return pData.Skip(4).ToArray();
                        }
                    }
                    throw new J2534Exception($"UDS Negative Response: Service=0x{serviceId:X2}, NRC=0x{nrc:X2} ({GetNrcDescription(nrc)})");
                }
            }
        }

        return Array.Empty<byte>();
    }

    public void StartSession(byte sessionType)
    {
        SendRequest(ServiceId.DiagnosticSessionControl, new[] { sessionType });
        Log.Information("UDS Session started: 0x{SessionType:X2}", sessionType);
    }

    public void TesterPresent()
    {
        SendRequest(ServiceId.TesterPresent, new byte[] { 0x00 });
    }

    public byte[] ReadDataByIdentifier(ushort did)
    {
        var response = SendRequest(ServiceId.ReadDataByIdentifier,
            new[] { (byte)(did >> 8), (byte)did });
        return response.Length > 3 ? response.Skip(3).ToArray() : Array.Empty<byte>();
    }

    public string ReadStringDid(ushort did)
    {
        var data = ReadDataByIdentifier(did);
        return data.Length > 0
            ? new string(data.Select(b => b >= 0x20 && b <= 0x7E ? (char)b : ' ').ToArray()).Trim()
            : string.Empty;
    }

    public EcuInformation ReadEcuInfo()
    {
        var info = new EcuInformation();

        try { info.Vin = ReadStringDid(DataIdentifier.VIN); } catch { }
        try { info.EcuName = ReadStringDid(DataIdentifier.SystemName); } catch { }
        try { info.Manufacturer = ReadStringDid(DataIdentifier.ECUManufacturerName); } catch { }
        try { info.SerialNumber = ReadStringDid(DataIdentifier.ECUSerialNumber); } catch { }
        try { info.PartNumber = ReadStringDid(DataIdentifier.VehicleManufacturerPartNumber); } catch { }
        try { info.SoftwareVersion = ReadStringDid(DataIdentifier.SystemSupplierECUSoftwareNumber); } catch { }
        try { info.HardwareVersion = ReadStringDid(DataIdentifier.SystemSupplierECUHardwareNumber); } catch { }
        try { info.CalibrationId = ReadStringDid(DataIdentifier.CalibrationId); } catch { }

        return info;
    }

    public List<DiagnosticTroubleCode> ReadDtcs()
    {
        var dtcs = new List<DiagnosticTroubleCode>();

        try
        {
            var response = SendRequest(ServiceId.ReadDtcInformation, new byte[] { 0x02, 0xFF });
            if (response.Length > 3)
            {
                for (int i = 3; i + 3 < response.Length; i += 4)
                {
                    uint dtcNumber = (uint)((response[i] << 16) | (response[i + 1] << 8) | response[i + 2]);
                    byte statusByte = response[i + 3];

                    var code = DecodeUdsDtc(dtcNumber);
                    dtcs.Add(new DiagnosticTroubleCode
                    {
                        Code = code,
                        Description = KnownDtcCodes.Descriptions.GetValueOrDefault(code, "Nieznany kod bledu"),
                        Status = (statusByte & 0x01) != 0 ? DtcStatus.Active : DtcStatus.Stored,
                        Module = "ECU"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read UDS DTCs");
        }

        return dtcs;
    }

    public bool ClearDtcs()
    {
        try
        {
            SendRequest(ServiceId.ClearDiagnosticInformation, new byte[] { 0xFF, 0xFF, 0xFF });
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to clear UDS DTCs");
            return false;
        }
    }

    public void EcuReset(byte resetType = 0x01)
    {
        SendRequest(ServiceId.EcuReset, new[] { resetType });
    }

    public byte[] SecurityAccess(byte accessLevel, byte[]? securityKey = null)
    {
        if (securityKey == null)
        {
            return SendRequest(ServiceId.SecurityAccess, new[] { accessLevel });
        }
        else
        {
            var data = new byte[securityKey.Length + 1];
            data[0] = (byte)(accessLevel + 1);
            Array.Copy(securityKey, 0, data, 1, securityKey.Length);
            return SendRequest(ServiceId.SecurityAccess, data);
        }
    }

    public void WriteDataByIdentifier(ushort did, byte[] data)
    {
        var payload = new byte[data.Length + 2];
        payload[0] = (byte)(did >> 8);
        payload[1] = (byte)did;
        Array.Copy(data, 0, payload, 2, data.Length);
        SendRequest(ServiceId.WriteDataByIdentifier, payload);
    }

    public byte[] RoutineControl(byte controlType, ushort routineId, byte[]? routineOption = null)
    {
        var data = new List<byte> { controlType, (byte)(routineId >> 8), (byte)routineId };
        if (routineOption != null)
            data.AddRange(routineOption);
        return SendRequest(ServiceId.RoutineControl, data.ToArray());
    }

    private static string DecodeUdsDtc(uint dtcNumber)
    {
        byte high = (byte)(dtcNumber >> 16);
        byte mid = (byte)(dtcNumber >> 8);
        byte low = (byte)dtcNumber;

        char prefix = ((high >> 6) & 0x03) switch
        {
            0 => 'P',
            1 => 'C',
            2 => 'B',
            3 => 'U',
            _ => 'P'
        };

        return $"{prefix}{(high >> 4) & 0x03}{high & 0x0F:X}{mid >> 4:X}{mid & 0x0F:X}";
    }

    private static string GetNrcDescription(byte nrc)
    {
        return nrc switch
        {
            0x10 => "General Reject",
            0x11 => "Service Not Supported",
            0x12 => "Sub-Function Not Supported",
            0x13 => "Incorrect Message Length",
            0x14 => "Response Too Long",
            0x22 => "Conditions Not Correct",
            0x24 => "Request Sequence Error",
            0x25 => "No Response From Subnet",
            0x26 => "Failure Prevents Execution",
            0x31 => "Request Out Of Range",
            0x33 => "Security Access Denied",
            0x35 => "Invalid Key",
            0x36 => "Exceeded Number Of Attempts",
            0x37 => "Required Time Delay Not Expired",
            0x70 => "Upload/Download Not Accepted",
            0x71 => "Transfer Data Suspended",
            0x72 => "General Programming Failure",
            0x73 => "Wrong Block Sequence Counter",
            0x78 => "Request Correctly Received - Response Pending",
            0x7E => "Sub-Function Not Supported In Active Session",
            0x7F => "Service Not Supported In Active Session",
            _ => $"Unknown NRC (0x{nrc:X2})"
        };
    }
}
