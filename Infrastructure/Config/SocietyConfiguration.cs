using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config
{
    public class SocietyConfiguration : IEntityTypeConfiguration<Society>
    {
        public void Configure(EntityTypeBuilder<Society> builder)
        {
            builder.Property(s => s.DefaultRatePerSqFt)
                .HasColumnType("decimal(18,2)");
        }
    }
}
