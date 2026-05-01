namespace SM3DiagTool.Models;

public class EcuInformation
{
    public string Vin { get; set; } = string.Empty;
    public string CalibrationId { get; set; } = string.Empty;
    public string EcuName { get; set; } = string.Empty;
    public string HardwareVersion { get; set; } = string.Empty;
    public string SoftwareVersion { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public uint DiagAddress { get; set; }
    public List<string> SupportedServices { get; set; } = new();
}

public class VehicleInfo
{
    public string Vin { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string EngineType { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public List<EcuInformation> DetectedEcus { get; set; } = new();
    public double BatteryVoltage { get; set; }
}
