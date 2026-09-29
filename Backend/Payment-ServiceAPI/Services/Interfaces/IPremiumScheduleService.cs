using Payment_ServiceAPI.DTOs.Premiums;

namespace Payment_ServiceAPI.Services.Interfaces;

public interface IPremiumScheduleService
{
    Task<IReadOnlyCollection<PremiumScheduleResponse>> GetAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PremiumScheduleResponse>> CreateAsync(CreatePremiumScheduleRequest request, string actor, CancellationToken cancellationToken = default);
}