using Application.DTOs.Complaints;
using Application.Exceptions;
using Domain.Enums;
using Domain.StateMachines;
using Microsoft.EntityFrameworkCore;
using Tests.TestHelpers;
using Xunit;

namespace Tests.ComplaintServiceTests
{
    public class TransitionAsyncTests
    {
        // Drives every (from, to) pair straight off ComplaintStateMachine.CanTransition — the
        // actual transition table — instead of hand-listing edges, so this keeps up automatically
        // if the table changes.
        public static IEnumerable<object[]> AllStatusPairs()
        {
            var statuses = Enum.GetValues<ComplaintStatus>();
            foreach (var from in statuses)
                foreach (var to in statuses)
                    yield return new object[] { from, to };
        }

        [Theory]
        [MemberData(nameof(AllStatusPairs))]
        public async Task Transition_MatchesStateMachine(ComplaintStatus from, ComplaintStatus to)
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            var complaint = TestData.NewComplaint(unit.Id, category.Id, status: from);
            db.Complaints.Add(complaint);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Resident(Guid.NewGuid(), unit.Id);
            var request = new TransitionComplaintRequest { ToStatus = to, Remarks = "remarks" };

            var isLegal = ComplaintStateMachine.CanTransition(from, to);

            if (isLegal)
            {
                var dto = await service.TransitionAsync(complaint.Id, request, actingUser);

                Assert.Equal(to, dto.Status);

                // History row lands in the same SaveChanges as the status change.
                var history = await db.ComplaintStatusHistories
                    .Where(h => h.ComplaintId == complaint.Id).ToListAsync();
                var row = Assert.Single(history);
                Assert.Equal(from, row.FromStatus);
                Assert.Equal(to, row.ToStatus);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidTransitionException>(
                    () => service.TransitionAsync(complaint.Id, request, actingUser));

                // A failed transition writes neither the status change nor a history row.
                Assert.Equal(from, complaint.Status);
                var history = await db.ComplaintStatusHistories
                    .Where(h => h.ComplaintId == complaint.Id).ToListAsync();
                Assert.Empty(history);
            }
        }

        [Fact]
        public async Task Transition_ToResolved_SetsResolvedAt()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            var complaint = TestData.NewComplaint(unit.Id, category.Id, status: ComplaintStatus.InProgress);
            db.Complaints.Add(complaint);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Resident(Guid.NewGuid(), unit.Id);

            var dto = await service.TransitionAsync(
                complaint.Id, new TransitionComplaintRequest { ToStatus = ComplaintStatus.Resolved, Remarks = null }, actingUser);

            Assert.NotNull(dto.ResolvedAt);
        }

        [Fact]
        public async Task Transition_ResolvedToReopened_ClearsResolvedAt()
        {
            // PROBE (a): ApplyTransition must clear ResolvedAt on any transition away from
            // Resolved. Resolved -> Reopened is a legal edge; a reopened complaint should read
            // as open again, not resolved-and-open at once.
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            var resolvedAt = DateTimeOffset.UtcNow.AddDays(-1);
            var complaint = TestData.NewComplaint(
                unit.Id, category.Id, status: ComplaintStatus.Resolved, resolvedAt: resolvedAt);
            db.Complaints.Add(complaint);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Resident(Guid.NewGuid(), unit.Id);

            var dto = await service.TransitionAsync(
                complaint.Id, new TransitionComplaintRequest { ToStatus = ComplaintStatus.Reopened, Remarks = null }, actingUser);

            Assert.Equal(ComplaintStatus.Reopened, dto.Status);
            Assert.Null(dto.ResolvedAt);
        }

        [Fact]
        public async Task Transition_UnknownComplaint_ThrowsNotFound()
        {
            using var db = AppDbContextFactory.Create();
            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());

            await Assert.ThrowsAsync<NotFoundException>(() => service.TransitionAsync(
                Guid.NewGuid(), new TransitionComplaintRequest { ToStatus = ComplaintStatus.Assigned, Remarks = null }, actingUser));
        }

        [Fact]
        public async Task Transition_ResidentOfDifferentUnit_ThrowsForbidden()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            var complaint = TestData.NewComplaint(unit.Id, category.Id, status: ComplaintStatus.Open);
            db.Complaints.Add(complaint);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Resident(Guid.NewGuid(), Guid.NewGuid());

            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.TransitionAsync(
                complaint.Id, new TransitionComplaintRequest { ToStatus = ComplaintStatus.Assigned, Remarks = null }, actingUser));
        }
    }
}
