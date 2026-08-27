using Payment_ServiceAPI.Data;
using Payment_ServiceAPI.Models.Audit;
using Payment_ServiceAPI.Repositories.Interfaces;

namespace Payment_ServiceAPI.Repositories.Implementations;

public class AuditEventRepository : IAuditEventRepository
{
    private readonly PaymentDbContext _context;

    public AuditEventRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        return _context.AuditEvents.AddAsync(auditEvent, cancellationToken).AsTask();
    }
}
