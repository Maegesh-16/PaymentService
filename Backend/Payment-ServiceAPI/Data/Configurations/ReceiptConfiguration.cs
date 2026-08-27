using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment_ServiceAPI.Models.Payments;

namespace Payment_ServiceAPI.Data.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts");
        builder.HasKey(x => x.ReceiptId);

        builder.Property(x => x.ReceiptNumber)
            .HasMaxLength(80)
            .IsRequired();

        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => x.ReceiptNumber).IsUnique();
    }
}
