using Payment_ServiceAPI.Models.Common;

namespace Payment_ServiceAPI.Models.Payments;

public class Payment : AuditableEntityBase
{
    public Guid PaymentId { get; set; }
    public Guid PolicyId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime PaymentDate { get; set; }

    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
