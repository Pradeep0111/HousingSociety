using Application.Exceptions;
using Tests.TestHelpers;
using Xunit;

namespace Tests.ComplaintServiceTests
{
    public class GetByIdAsyncTests
    {
        [Fact]
        public async Task GetById_Admin_Succeeds()
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
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());

            var dto = await service.GetByIdAsync(complaint.Id, actingUser);

            Assert.Equal(complaint.Id, dto.Id);
        }

        [Fact]
        public async Task GetById_ResidentOfDifferentUnit_ThrowsForbidden()
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

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => service.GetByIdAsync(complaint.Id, actingUser));
        }

        [Fact]
        public async Task GetById_UnknownComplaint_ThrowsNotFound()
        {
            using var db = AppDbContextFactory.Create();
            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid(), actingUser));
        }
    }
}
