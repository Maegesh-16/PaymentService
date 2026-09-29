using Payment_ServiceAPI.Models.Common;

namespace Payment_ServiceAPI.Models.Premiums;

public class PremiumSchedule : AuditableEntityBase
{
    public Guid ScheduleId { get; set; }
    public Guid PolicyId { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? PaymentId { get; set; }
    public DateTime? PaidDate { get; set; }
}