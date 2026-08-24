using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
    {
        public DbSet<Society> Societies => Set<Society>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<UnitOccupant> UnitOccupants => Set<UnitOccupant>();
        public DbSet<AmentitySlot> AmentitySlots => Set<AmentitySlot>();
        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<ComplaintCategory> ComplaintCategories => Set<ComplaintCategory>();
        public DbSet<ComplaintStatusHistory> ComplaintStatusHistories => Set<ComplaintStatusHistory>();
        public DbSet<MaintenanceCycle> MaintenanceCycles => Set<MaintenanceCycle>();
        public DbSet<DuesInvoice> DuesInvoices => Set<DuesInvoice>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<VisitorEntry> VisitorEntries => Set<VisitorEntry>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
