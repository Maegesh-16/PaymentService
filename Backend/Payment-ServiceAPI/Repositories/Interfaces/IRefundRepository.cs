using Payment_ServiceAPI.Models.Payments;

namespace Payment_ServiceAPI.Repositories.Interfaces;

public interface IRefundRepository
{
    Task AddAsync(Refund entity, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Refund>> GetAsync(Guid? paymentId, CancellationToken cancellationToken = default);
}
