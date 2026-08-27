using Payment_ServiceAPI.Models.Operations;

namespace Payment_ServiceAPI.Repositories.Interfaces;

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> GetAsync(string operation, string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
}
