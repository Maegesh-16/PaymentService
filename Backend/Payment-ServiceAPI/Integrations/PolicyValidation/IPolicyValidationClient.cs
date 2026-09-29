namespace Payment_ServiceAPI.Integrations.PolicyValidation;

public sealed record PolicyPaymentDetails(Guid PolicyId, DateTime StartDate, decimal PremiumAmount, int Status);

public interface IPolicyValidationClient
{
    Task<bool> PolicyExistsAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<PolicyPaymentDetails?> GetPolicyPaymentDetailsAsync(Guid policyId, CancellationToken cancellationToken = default);
}
