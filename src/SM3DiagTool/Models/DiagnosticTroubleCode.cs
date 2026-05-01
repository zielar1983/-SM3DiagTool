namespace SM3DiagTool.Models;

public class DiagnosticTroubleCode
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DtcStatus Status { get; set; }
    public string Module { get; set; } = string.Empty;
    public DateTime? FirstOccurrence { get; set; }

    public string StatusText => Status switch
    {
        DtcStatus.Active => "Aktywny",
        DtcStatus.Pending => "Oczekujacy",
        DtcStatus.Stored => "Zapisany",
        DtcStatus.Permanent => "Permanentny",
        _ => "Nieznany"
    };

    public override string ToString() => $"{Code} - {Description} [{StatusText}]";
}

public enum DtcStatus
{
    Active,
    Pending,
    Stored,
    Permanent
}
