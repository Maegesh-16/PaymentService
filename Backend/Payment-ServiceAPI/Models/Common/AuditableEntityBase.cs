namespace Payment_ServiceAPI.Models.Common;

public abstract class AuditableEntityBase
{
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
}
