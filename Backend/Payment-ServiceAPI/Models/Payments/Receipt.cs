using Payment_ServiceAPI.Models.Common;

namespace Payment_ServiceAPI.Models.Payments;

public class Receipt : AuditableEntityBase
{
    public Guid ReceiptId { get; set; }
    public Guid PaymentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }

    public Payment? Payment { get; set; }
}
