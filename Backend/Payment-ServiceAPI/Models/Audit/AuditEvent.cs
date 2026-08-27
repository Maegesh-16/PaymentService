namespace Payment_ServiceAPI.Models.Audit;

public class AuditEvent
{
    public Guid AuditEventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Actor { get; set; } = "system";
    public string CorrelationId { get; set; } = string.Empty;
    public string Details { get; set; } = "{}";
    public DateTime OccurredAtUtc { get; set; }
}
