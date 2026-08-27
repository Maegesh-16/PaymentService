namespace Payment_ServiceAPI.Integrations.PolicyValidation;

public class PolicyValidationOptions
{
    public const string SectionName = "PolicyValidation";

    public string BaseUrl { get; set; } = string.Empty;

    public string ExistsEndpointTemplate { get; set; } = "/api/policies/{policyId}";

    public int RequestTimeoutSeconds { get; set; } = 5;

    public int RetryCount { get; set; } = 2;

    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    public bool AllowFallbackKnownPolicies { get; set; } = true;

    public List<Guid> KnownPolicyIds { get; set; } = new();
}
