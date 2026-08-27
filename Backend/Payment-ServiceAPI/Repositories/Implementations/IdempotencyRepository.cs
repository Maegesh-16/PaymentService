using Microsoft.EntityFrameworkCore;
using Payment_ServiceAPI.Data;
using Payment_ServiceAPI.Models.Operations;
using Payment_ServiceAPI.Repositories.Interfaces;

namespace Payment_ServiceAPI.Repositories.Implementations;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly PaymentDbContext _context;

    public IdempotencyRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public Task<IdempotencyRecord?> GetAsync(string operation, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return _context.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Operation == operation && x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        return _context.IdempotencyRecords.AddAsync(record, cancellationToken).AsTask();
    }
}
