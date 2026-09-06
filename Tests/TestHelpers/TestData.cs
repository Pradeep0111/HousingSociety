using Domain.Entities;
using Domain.Enums;

namespace Tests.TestHelpers
{
    public static class TestData
    {
        public static Unit NewUnit(Guid? id = null) => new()
        {
            Id = id ?? Guid.NewGuid(),
            SocietyId = Guid.NewGuid(),
            BlockName = "A",
            UnitNumber = "101",
            OwnerId = Guid.NewGuid(),
        };

        public static ComplaintCategory NewCategory(int slaHours = 24, Guid? id = null) => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Plumbing",
            SlaHours = slaHours,
        };

        public static ApplicationUser NewUser(Guid? id = null)
        {
            var userId = id ?? Guid.NewGuid();
            var tag = userId.ToString();
            return new ApplicationUser
            {
                Id = userId,
                FullName = "Test User",
                UserName = $"user-{tag}",
                NormalizedUserName = $"USER-{tag}".ToUpperInvariant(),
                Email = $"{tag}@test.local",
                NormalizedEmail = $"{tag}@test.local".ToUpperInvariant(),
            };
        }

        public static Complaint NewComplaint(
            Guid unitId,
            Guid categoryId,
            ComplaintStatus status = ComplaintStatus.Open,
            Guid? raisedBy = null,
            DateTimeOffset? createdAt = null,
            DateTimeOffset? resolvedAt = null) => new()
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            ComplaintCategoryId = categoryId,
            RaisedByUserId = raisedBy ?? Guid.NewGuid(),
            Description = "Leaking tap",
            Status = status,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            SlaDeadline = (createdAt ?? DateTimeOffset.UtcNow).AddHours(24),
            ResolvedAt = resolvedAt,
        };
    }
}
