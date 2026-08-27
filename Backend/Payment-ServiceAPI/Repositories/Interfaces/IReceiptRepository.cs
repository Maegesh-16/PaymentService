using Payment_ServiceAPI.Models.Payments;

namespace Payment_ServiceAPI.Repositories.Interfaces;

public interface IReceiptRepository
{
    Task AddAsync(Receipt entity, CancellationToken cancellationToken = default);

    Task<bool> ReceiptNumberExistsAsync(string receiptNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Receipt>> GetAsync(Guid? paymentId, CancellationToken cancellationToken = default);
}
