using System.Runtime.InteropServices;

namespace SM3DiagTool.J2534;

[StructLayout(LayoutKind.Sequential)]
public struct PassThruMsg
{
    public uint ProtocolID;
    public uint RxStatus;
    public uint TxFlags;
    public uint Timestamp;
    public uint DataSize;
    public uint ExtraDataIndex;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4128)]
    public byte[] Data;

    public PassThruMsg(J2534Protocol protocol)
    {
        ProtocolID = (uint)protocol;
        RxStatus = 0;
        TxFlags = 0;
        Timestamp = 0;
        DataSize = 0;
        ExtraDataIndex = 0;
        Data = new byte[4128];
    }

    public void SetData(byte[] data)
    {
        Array.Copy(data, Data, data.Length);
        DataSize = (uint)data.Length;
    }

    public byte[] GetData()
    {
        var result = new byte[DataSize];
        Array.Copy(Data, result, DataSize);
        return result;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct SConfig
{
    public uint Parameter;
    public uint Value;
}

[StructLayout(LayoutKind.Sequential)]
public struct SConfigList
{
    public uint NumOfParams;
    public IntPtr ConfigPtr;
}

[StructLayout(LayoutKind.Sequential)]
public struct SByteArray
{
    public uint NumOfBytes;
    public IntPtr BytePtr;
}

public class J2534DeviceInfo
{
    public string Name { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string DllPath { get; set; } = string.Empty;
    public string ConfigApp { get; set; } = string.Empty;
    public List<J2534Protocol> SupportedProtocols { get; set; } = new();

    public override string ToString() => $"{Name} ({Vendor})";
}
