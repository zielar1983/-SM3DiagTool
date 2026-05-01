namespace SM3DiagTool.Models;

public class CanMessage
{
    public uint Id { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsExtendedId { get; set; }
    public bool IsTx { get; set; }
    public int DataLength => Data.Length;

    public string IdHex => IsExtendedId ? $"0x{Id:X8}" : $"0x{Id:X3}";
    public string DataHex => BitConverter.ToString(Data).Replace("-", " ");
    public string DataAscii => new(Data.Select(b => b >= 0x20 && b <= 0x7E ? (char)b : '.').ToArray());
    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss.fff");
    public string Direction => IsTx ? "TX" : "RX";

    public override string ToString() => $"[{TimestampFormatted}] {Direction} {IdHex} [{DataLength}] {DataHex}";
}
