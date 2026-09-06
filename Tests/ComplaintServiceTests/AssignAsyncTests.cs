using Application.DTOs.Complaints;
using Application.Exceptions;
using Domain.Enums;
using Tests.TestHelpers;
using Xunit;

namespace Tests.ComplaintServiceTests
{
    public class AssignAsyncTests
    {
        [Fact]
        public async Task Assign_LegalState_SetsAssigneeAndStatus()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            var assignee = TestData.NewUser();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            db.Users.Add(assignee);
            var complaint = TestData.NewComplaint(unit.Id, category.Id, status: ComplaintStatus.Open);
            db.Complaints.Add(complaint);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());

            var dto = await service.AssignAsync(
                complaint.Id, new AssignComplaintRequest { AssignedToUserId = assignee.Id, Remarks = null }, actingUser);

            Assert.Equal(ComplaintStatus.Assigned, dto.Status);
            Assert.Equal(assignee.Id, dto.AssignedToUserId);
        }

        [Fact]
        public async Task Assign_UnknownAssignee_ThrowsNotFound()
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
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());

            await Assert.ThrowsAsync<NotFoundException>(() => service.AssignAsync(
                complaint.Id, new AssignComplaintRequest { AssignedToUserId = Guid.NewGuid(), Remarks = null }, actingUser));
        }

        [Fact]
        public async Task Assign_IllegalState_ThrowsInvalidTransition()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            var assignee = TestData.NewUser();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            db.Users.Add(assignee);
            // Resolved cannot transition to Assigned per the state machine.
            var complaint = TestData.NewComplaint(unit.Id, category.Id, status: ComplaintStatus.Resolved);
            db.Complaints.Add(complaint);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());

            await Assert.ThrowsAsync<InvalidTransitionException>(() => service.AssignAsync(
                complaint.Id, new AssignComplaintRequest { AssignedToUserId = assignee.Id, Remarks = null }, actingUser));
        }

        [Fact]
        public async Task Assign_IllegalStateWithUnknownAssignee_ThrowsInvalidTransition_NotNotFound()
        {
            // PROBE (c): AssignAsync checks CanTransition BEFORE looking up the assignee, so
            // assigning a nonexistent user to a complaint in an illegal state surfaces as
            // InvalidTransitionException (409), not NotFoundException (404). Pinning this so the
            // ordering can't drift silently; god flagged it as "probably fine".
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            var complaint = TestData.NewComplaint(unit.Id, category.Id, status: ComplaintStatus.Resolved);
            db.Complaints.Add(complaint);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());

            await Assert.ThrowsAsync<InvalidTransitionException>(() => service.AssignAsync(
                complaint.Id, new AssignComplaintRequest { AssignedToUserId = Guid.NewGuid(), Remarks = null }, actingUser));
        }

        [Fact]
        public async Task Assign_ResidentOfDifferentUnit_ThrowsForbidden()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            var complaint = TestData.NewComplaint(unit.Id, category.Id, status: ComplaintStatus.Open);
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            db.Complaints.Add(complaint);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Resident(Guid.NewGuid(), Guid.NewGuid());

            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.AssignAsync(
                complaint.Id, new AssignComplaintRequest { AssignedToUserId = Guid.NewGuid(), Remarks = null }, actingUser));
        }
    }
}
