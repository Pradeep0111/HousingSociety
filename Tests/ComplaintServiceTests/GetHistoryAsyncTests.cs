using Domain.Enums;
using Domain.Entities;
using Tests.TestHelpers;
using Xunit;

namespace Tests.ComplaintServiceTests
{
    public class GetHistoryAsyncTests
    {
        [Fact]
        public async Task History_ReturnsOldestFirst_OnlyForRequestedComplaint()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            var target = TestData.NewComplaint(unit.Id, category.Id);
            var other = TestData.NewComplaint(unit.Id, category.Id);
            db.Complaints.AddRange(target, other);

            var now = DateTimeOffset.UtcNow;
            var newest = NewHistoryRow(target.Id, ComplaintStatus.Assigned, ComplaintStatus.InProgress, now);
            var oldest = NewHistoryRow(target.Id, ComplaintStatus.Open, ComplaintStatus.Assigned, now.AddHours(-2));
            var middle = NewHistoryRow(target.Id, ComplaintStatus.Open, ComplaintStatus.Open, now.AddHours(-1));
            var unrelated = NewHistoryRow(other.Id, ComplaintStatus.Open, ComplaintStatus.Open, now.AddHours(-5));
            // Inserted out of chronological order on purpose.
            db.ComplaintStatusHistories.AddRange(newest, oldest, middle, unrelated);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Resident(Guid.NewGuid(), unit.Id);

            var history = await service.GetHistoryAsync(target.Id, actingUser);

            Assert.Equal([oldest.Id, middle.Id, newest.Id], history.Select(h => h.Id).ToArray());
        }

        [Fact]
        public async Task History_ResidentOfDifferentUnit_ThrowsForbidden()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            var complaint = TestData.NewComplaint(unit.Id, category.Id);
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            db.Complaints.Add(complaint);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Resident(Guid.NewGuid(), Guid.NewGuid());

            await Assert.ThrowsAsync<Application.Exceptions.ForbiddenAccessException>(
                () => service.GetHistoryAsync(complaint.Id, actingUser));
        }

        private static ComplaintStatusHistory NewHistoryRow(
            Guid complaintId, ComplaintStatus from, ComplaintStatus to, DateTimeOffset changedAt) => new()
        {
            Id = Guid.NewGuid(),
            ComplaintId = complaintId,
            FromStatus = from,
            ToStatus = to,
            ChangedByUserId = Guid.NewGuid(),
            ChangedAt = changedAt,
        };
    }
}
