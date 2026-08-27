using Payment_ServiceAPI.Models.Common;

namespace Payment_ServiceAPI.Models.Payments;

public class Refund : AuditableEntityBase
{
    public Guid RefundId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Requested";
    public DateTime RefundDate { get; set; }

    public Payment? Payment { get; set; }
}
