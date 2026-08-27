using Payment_ServiceAPI.Models.Common;

namespace Payment_ServiceAPI.Models.Payments;

public class PaymentTransaction : AuditableEntityBase
{
    public Guid TransactionId { get; set; }
    public Guid PaymentId { get; set; }
    public string GatewayRef { get; set; } = string.Empty;
    public string Status { get; set; } = "Initiated";

    public Payment? Payment { get; set; }
}
