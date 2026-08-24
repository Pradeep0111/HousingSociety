using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config
{
    public class MaintenanceCycleConfiguration : IEntityTypeConfiguration<MaintenanceCycle>
    {
        public void Configure(EntityTypeBuilder<MaintenanceCycle> builder)
        {
            builder.Property(m => m.RatePerSqFt)
                .HasColumnType("decimal(18,2)");
        }
    }
}
