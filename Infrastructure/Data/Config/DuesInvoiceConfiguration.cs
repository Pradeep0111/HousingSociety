using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config
{
    public class DuesInvoiceConfiguration : IEntityTypeConfiguration<DuesInvoice>
    {
        public void Configure(EntityTypeBuilder<DuesInvoice> builder)
        {
            builder.Property(d => d.BaseAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(d => d.LateFeeAmount)
                .HasColumnType("decimal(18,2)");

            builder.Ignore(d => d.TotalAmount);
        }
    }
}
