using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment_ServiceAPI.Models.Payments;

namespace Payment_ServiceAPI.Data.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(x => x.TransactionId);

        builder.Property(x => x.GatewayRef)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => x.GatewayRef).IsUnique();
    }
}
