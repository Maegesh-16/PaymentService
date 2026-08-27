using Microsoft.EntityFrameworkCore;
using Payment_ServiceAPI.Data;
using Payment_ServiceAPI.Models.Payments;
using Payment_ServiceAPI.Repositories.Interfaces;

namespace Payment_ServiceAPI.Repositories.Implementations;

public class ReceiptRepository : IReceiptRepository
{
    private readonly PaymentDbContext _context;

    public ReceiptRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Receipt entity, CancellationToken cancellationToken = default)
    {
        return _context.Receipts.AddAsync(entity, cancellationToken).AsTask();
    }

    public Task<bool> ReceiptNumberExistsAsync(string receiptNumber, CancellationToken cancellationToken = default)
    {
        return _context.Receipts.AnyAsync(x => x.ReceiptNumber == receiptNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Receipt>> GetAsync(Guid? paymentId, CancellationToken cancellationToken = default)
    {
        var query = _context.Receipts.AsNoTracking();

        if (paymentId.HasValue)
        {
            query = query.Where(x => x.PaymentId == paymentId.Value);
        }

        return await query
            .OrderByDescending(x => x.GeneratedDate)
            .ToListAsync(cancellationToken);
    }
}
