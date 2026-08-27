using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment_ServiceAPI.Models.Audit;

namespace Payment_ServiceAPI.Data.Configurations;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");
        builder.HasKey(x => x.AuditEventId);

        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Actor).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
    }
}
