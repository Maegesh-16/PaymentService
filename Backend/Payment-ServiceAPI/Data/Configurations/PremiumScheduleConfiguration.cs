using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment_ServiceAPI.Models.Premiums;

namespace Payment_ServiceAPI.Data.Configurations;

public class PremiumScheduleConfiguration : IEntityTypeConfiguration<PremiumSchedule>
{
    public void Configure(EntityTypeBuilder<PremiumSchedule> builder)
    {
        builder.ToTable("PremiumSchedules");
        builder.HasKey(x => x.ScheduleId);

        builder.Property(x => x.Frequency)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => new { x.PolicyId, x.InstallmentNumber })
            .IsUnique();
    }
}