using Microsoft.EntityFrameworkCore;
using Payment_ServiceAPI.Data;
using Payment_ServiceAPI.Models.Payments;
using Payment_ServiceAPI.Repositories.Interfaces;

namespace Payment_ServiceAPI.Repositories.Implementations;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Payment entity, CancellationToken cancellationToken = default)
    {
        return _context.Payments.AddAsync(entity, cancellationToken).AsTask();
    }

    public Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        return _context.Payments.FirstOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetAsync(Guid? policyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments.AsNoTracking();

        if (policyId.HasValue)
        {
            query = query.Where(x => x.PolicyId == policyId.Value);
        }

        return await query
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
