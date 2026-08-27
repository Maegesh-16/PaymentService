using Payment_ServiceAPI.Models.Payments;

namespace Payment_ServiceAPI.Repositories.Interfaces;

public interface IPaymentTransactionRepository
{
    Task AddAsync(PaymentTransaction entity, CancellationToken cancellationToken = default);

    Task<bool> GatewayRefExistsAsync(string gatewayRef, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTransaction>> GetAsync(Guid? paymentId, CancellationToken cancellationToken = default);
}
