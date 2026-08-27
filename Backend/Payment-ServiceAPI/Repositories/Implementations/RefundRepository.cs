using Microsoft.EntityFrameworkCore;
using Payment_ServiceAPI.Data;
using Payment_ServiceAPI.Models.Payments;
using Payment_ServiceAPI.Repositories.Interfaces;

namespace Payment_ServiceAPI.Repositories.Implementations;

public class RefundRepository : IRefundRepository
{
    private readonly PaymentDbContext _context;

    public RefundRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Refund entity, CancellationToken cancellationToken = default)
    {
        return _context.Refunds.AddAsync(entity, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<Refund>> GetAsync(Guid? paymentId, CancellationToken cancellationToken = default)
    {
        var query = _context.Refunds.AsNoTracking();

        if (paymentId.HasValue)
        {
            query = query.Where(x => x.PaymentId == paymentId.Value);
        }

        return await query
            .OrderByDescending(x => x.RefundDate)
            .ToListAsync(cancellationToken);
    }
}
