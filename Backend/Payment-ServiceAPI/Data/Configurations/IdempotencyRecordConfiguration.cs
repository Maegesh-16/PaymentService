using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment_ServiceAPI.Models.Operations;

namespace Payment_ServiceAPI.Data.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(x => x.IdempotencyRecordId);

        builder.Property(x => x.Operation).HasMaxLength(120).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ResponsePayload).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.Operation, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
