namespace Payment_ServiceAPI.Models.Operations;

public class IdempotencyRecord
{
    public Guid IdempotencyRecordId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string ResponsePayload { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
