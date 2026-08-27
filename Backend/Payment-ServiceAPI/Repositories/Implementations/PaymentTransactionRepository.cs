using Microsoft.EntityFrameworkCore;
using Payment_ServiceAPI.Data;
using Payment_ServiceAPI.Models.Payments;
using Payment_ServiceAPI.Repositories.Interfaces;

namespace Payment_ServiceAPI.Repositories.Implementations;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly PaymentDbContext _context;

    public PaymentTransactionRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(PaymentTransaction entity, CancellationToken cancellationToken = default)
    {
        return _context.PaymentTransactions.AddAsync(entity, cancellationToken).AsTask();
    }

    public Task<bool> GatewayRefExistsAsync(string gatewayRef, CancellationToken cancellationToken = default)
    {
        return _context.PaymentTransactions.AnyAsync(x => x.GatewayRef == gatewayRef, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentTransaction>> GetAsync(Guid? paymentId, CancellationToken cancellationToken = default)
    {
        var query = _context.PaymentTransactions.AsNoTracking();

        if (paymentId.HasValue)
        {
            query = query.Where(x => x.PaymentId == paymentId.Value);
        }

        return await query
            .OrderByDescending(x => x.TransactionId)
            .ToListAsync(cancellationToken);
    }
}
