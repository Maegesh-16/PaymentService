using Payment_ServiceAPI.Models.Payments;

namespace Payment_ServiceAPI.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment entity, CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetAsync(Guid? policyId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
