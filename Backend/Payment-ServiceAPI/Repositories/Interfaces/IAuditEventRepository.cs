using Payment_ServiceAPI.Models.Audit;

namespace Payment_ServiceAPI.Repositories.Interfaces;

public interface IAuditEventRepository
{
    Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
