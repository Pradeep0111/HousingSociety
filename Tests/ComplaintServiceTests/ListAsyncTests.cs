using Application.DTOs.Complaints;
using Application.Exceptions;
using Domain.Enums;
using Tests.TestHelpers;
using Xunit;

namespace Tests.ComplaintServiceTests
{
    public class ListAsyncTests
    {
        [Fact]
        public async Task List_ResidentRequestedUnitIdIsIgnored_ReturnsOnlyOwnUnit()
        {
            using var db = AppDbContextFactory.Create();
            var ownUnit = TestData.NewUnit();
            var otherUnit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.AddRange(ownUnit, otherUnit);
            db.ComplaintCategories.Add(category);
            var ownComplaint = TestData.NewComplaint(ownUnit.Id, category.Id);
            var otherComplaint = TestData.NewComplaint(otherUnit.Id, category.Id);
            db.Complaints.AddRange(ownComplaint, otherComplaint);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Resident(Guid.NewGuid(), ownUnit.Id);
            // Security-relevant: resident asks for the OTHER unit's complaints...
            var query = new ComplaintListQuery { UnitId = otherUnit.Id };

            var result = await service.ListAsync(query, actingUser);

            // ...and gets back their OWN unit's rows instead, never the other unit's.
            var item = Assert.Single(result.Items);
            Assert.Equal(ownComplaint.Id, item.Id);
        }

        [Fact]
        public async Task List_UserWithNoUnitClaim_ThrowsForbidden()
        {
            using var db = AppDbContextFactory.Create();
            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.WithoutUnit(Guid.NewGuid());

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => service.ListAsync(new ComplaintListQuery(), actingUser));
        }

        [Fact]
        public async Task List_AdminWithNoFilter_SeesAllUnits()
        {
            using var db = AppDbContextFactory.Create();
            var unitA = TestData.NewUnit();
            var unitB = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.AddRange(unitA, unitB);
            db.ComplaintCategories.Add(category);
            db.Complaints.AddRange(
                TestData.NewComplaint(unitA.Id, category.Id),
                TestData.NewComplaint(unitB.Id, category.Id));
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());

            var result = await service.ListAsync(new ComplaintListQuery(), actingUser);

            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task List_AdminWithUnitFilter_FiltersToThatUnit()
        {
            using var db = AppDbContextFactory.Create();
            var unitA = TestData.NewUnit();
            var unitB = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.AddRange(unitA, unitB);
            db.ComplaintCategories.Add(category);
            var complaintA = TestData.NewComplaint(unitA.Id, category.Id);
            db.Complaints.AddRange(complaintA, TestData.NewComplaint(unitB.Id, category.Id));
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());

            var result = await service.ListAsync(new ComplaintListQuery { UnitId = unitA.Id }, actingUser);

            var item = Assert.Single(result.Items);
            Assert.Equal(complaintA.Id, item.Id);
        }

        [Fact]
        public async Task List_OrdersByCreatedAtDescending()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            var now = DateTimeOffset.UtcNow;
            var oldest = TestData.NewComplaint(unit.Id, category.Id, createdAt: now.AddDays(-2));
            var middle = TestData.NewComplaint(unit.Id, category.Id, createdAt: now.AddDays(-1));
            var newest = TestData.NewComplaint(unit.Id, category.Id, createdAt: now);
            db.Complaints.AddRange(middle, oldest, newest); // inserted out of order on purpose
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());

            var result = await service.ListAsync(new ComplaintListQuery(), actingUser);

            Assert.Equal([newest.Id, middle.Id, oldest.Id], result.Items.Select(i => i.Id).ToArray());
        }

        [Fact]
        public async Task List_DefaultsPageAndPageSize_AndReportsTotalCount()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            for (var i = 0; i < 3; i++)
                db.Complaints.Add(TestData.NewComplaint(unit.Id, category.Id));
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());
            // Page/PageSize below the service's own valid range (0), so it must fall back to 1/20.
            var query = new ComplaintListQuery { Page = 0, PageSize = 0 };

            var result = await service.ListAsync(query, actingUser);

            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
        }

        [Fact]
        public async Task List_PageSizeAboveMax_ClampsToTwoHundred()
        {
            // PROBE (b): an out-of-range PageSize must clamp to the max (200) when it's too
            // high, and only fall back to the default (20) when it's too low (< 1).
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());
            var query = new ComplaintListQuery { PageSize = 500 };

            var result = await service.ListAsync(query, actingUser);

            Assert.Equal(200, result.PageSize);
        }
    }
}
