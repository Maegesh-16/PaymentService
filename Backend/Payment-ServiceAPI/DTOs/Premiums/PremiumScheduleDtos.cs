using System.ComponentModel.DataAnnotations;

namespace Payment_ServiceAPI.DTOs.Premiums;

/// <summary>Payload for creating all premium installments for a policy.</summary>
public sealed record CreatePremiumScheduleRequest(
    Guid PolicyId,
    [Required, RegularExpression("^(Monthly|Quarterly|HalfYearly|Annual)$")] string Frequency);

/// <summary>Represents one premium installment for a policy.</summary>
public sealed record PremiumScheduleResponse(
    Guid ScheduleId,
    Guid PolicyId,
    int InstallmentNumber,
    DateTime DueDate,
    decimal Amount,
    string Status,
    Guid? PaymentId,
    DateTime? PaidDate);