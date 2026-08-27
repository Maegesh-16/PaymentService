namespace Payment_ServiceAPI.Integrations.PolicyValidation;

public interface IPolicyValidationClient
{
    Task<bool> PolicyExistsAsync(Guid policyId, CancellationToken cancellationToken = default);
}
