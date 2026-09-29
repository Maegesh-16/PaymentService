using Microsoft.EntityFrameworkCore;
using Payment_ServiceAPI.Data;
using Payment_ServiceAPI.DTOs.Premiums;
using Payment_ServiceAPI.Integrations.PolicyValidation;
using Payment_ServiceAPI.Models.Premiums;
using Payment_ServiceAPI.Services.Interfaces;

namespace Payment_ServiceAPI.Services.Implementations;

public sealed class PremiumScheduleService : IPremiumScheduleService
{
    private static readonly IReadOnlyDictionary<string, (int Count, int MonthInterval)> Frequencies =
        new Dictionary<string, (int Count, int MonthInterval)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Monthly"] = (12, 1),
            ["Quarterly"] = (4, 3),
            ["HalfYearly"] = (2, 6),
            ["Annual"] = (1, 12)
        };

    private readonly PaymentDbContext _dbContext;
    private readonly IPolicyValidationClient _policyClient;

    public PremiumScheduleService(PaymentDbContext dbContext, IPolicyValidationClient policyClient)
    {
        _dbContext = dbContext;
        _policyClient = policyClient;
    }

    public async Task<IReadOnlyCollection<PremiumScheduleResponse>> GetAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PremiumSchedules
            .AsNoTracking()
            .Where(x => x.PolicyId == policyId)
            .OrderBy(x => x.InstallmentNumber)
            .Select(x => ToResponse(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PremiumScheduleResponse>> CreateAsync(
        CreatePremiumScheduleRequest request,
        string actor,
        CancellationToken cancellationToken = default)
    {
        if (!Frequencies.TryGetValue(request.Frequency, out var frequency))
        {
            throw new InvalidOperationException($"Payment frequency '{request.Frequency}' is not supported.");
        }

        if (await _dbContext.PremiumSchedules.AnyAsync(x => x.PolicyId == request.PolicyId, cancellationToken))
        {
            throw new InvalidOperationException($"A payment schedule already exists for policy '{request.PolicyId}'.");
        }

        var policy = await _policyClient.GetPolicyPaymentDetailsAsync(request.PolicyId, cancellationToken)
            ?? throw new InvalidOperationException($"Policy '{request.PolicyId}' was not found.");

        if (policy.Status != 3)
        {
            throw new InvalidOperationException("Only active policies can have a payment schedule.");
        }

        if (policy.PremiumAmount <= 0)
        {
            throw new InvalidOperationException("The policy premium must be greater than zero.");
        }

        var standardAmount = decimal.Round(policy.PremiumAmount / frequency.Count, 2, MidpointRounding.AwayFromZero);
        var createdAt = DateTime.UtcNow;
        var schedules = Enumerable.Range(0, frequency.Count)
            .Select(index => new PremiumSchedule
            {
                ScheduleId = Guid.NewGuid(),
                PolicyId = request.PolicyId,
                Frequency = Frequencies.Keys.First(key => key.Equals(request.Frequency, StringComparison.OrdinalIgnoreCase)),
                InstallmentNumber = index + 1,
                DueDate = policy.StartDate.Date.AddMonths(index * frequency.MonthInterval),
                Amount = index == frequency.Count - 1
                    ? policy.PremiumAmount - (standardAmount * (frequency.Count - 1))
                    : standardAmount,
                Status = "Pending",
                CreatedAtUtc = createdAt,
                CreatedBy = actor
            })
            .ToList();

        await _dbContext.PremiumSchedules.AddRangeAsync(schedules, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return schedules.Select(ToResponse).ToList();
    }

    private static PremiumScheduleResponse ToResponse(PremiumSchedule schedule) => new(
        schedule.ScheduleId,
        schedule.PolicyId,
        schedule.InstallmentNumber,
        schedule.DueDate,
        schedule.Amount,
        schedule.Status,
        schedule.PaymentId,
        schedule.PaidDate);
}