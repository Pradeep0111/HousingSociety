using Application.DTOs.Complaints;
using Application.Exceptions;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Tests.TestHelpers;
using Xunit;

namespace Tests.ComplaintServiceTests
{
    public class CreateAsyncTests
    {
        [Fact]
        public async Task Create_SetsSlaDeadline_Status_RaisedBy_AndWritesInitialHistory()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory(slaHours: 48);
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var userId = Guid.NewGuid();
            var actingUser = ClaimsPrincipalFactory.Resident(userId, unit.Id);
            var request = new CreateComplaintRequest { UnitId = unit.Id, ComplaintCategoryId = category.Id, Description = "Leaking tap in kitchen" };

            var dto = await service.CreateAsync(request, actingUser);

            Assert.Equal(ComplaintStatus.Open, dto.Status);
            Assert.Equal(userId, dto.RaisedByUserId);
            Assert.Equal(dto.CreatedAt.AddHours(48), dto.SlaDeadline);

            var history = await db.ComplaintStatusHistories.Where(h => h.ComplaintId == dto.Id).ToListAsync();
            var row = Assert.Single(history);
            Assert.Equal(ComplaintStatus.Open, row.FromStatus);
            Assert.Equal(ComplaintStatus.Open, row.ToStatus);
            Assert.Equal(userId, row.ChangedByUserId);
        }

        // CreateComplaintRequest has no RaisedByUserId field at all, so there is nothing in the
        // body that could override the token's identity — RaisedByUserId can only ever come from
        // GetUserId(actingUser). The assertion above already pins that.

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_MissingOrWhitespaceDescription_ThrowsValidation(string description)
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Resident(Guid.NewGuid(), unit.Id);
            var request = new CreateComplaintRequest { UnitId = unit.Id, ComplaintCategoryId = category.Id, Description = description };

            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request, actingUser));
        }

        [Fact]
        public async Task Create_UnknownUnit_ThrowsNotFound()
        {
            using var db = AppDbContextFactory.Create();
            var category = TestData.NewCategory();
            db.ComplaintCategories.Add(category);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Admin(Guid.NewGuid());
            var request = new CreateComplaintRequest { UnitId = Guid.NewGuid(), ComplaintCategoryId = category.Id, Description = "Something broke" };

            await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(request, actingUser));
        }

        [Fact]
        public async Task Create_UnknownCategory_ThrowsNotFound()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            db.Units.Add(unit);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            var actingUser = ClaimsPrincipalFactory.Resident(Guid.NewGuid(), unit.Id);
            var request = new CreateComplaintRequest { UnitId = unit.Id, ComplaintCategoryId = Guid.NewGuid(), Description = "Something broke" };

            await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(request, actingUser));
        }

        [Fact]
        public async Task Create_ResidentOfDifferentUnit_ThrowsForbidden()
        {
            using var db = AppDbContextFactory.Create();
            var unit = TestData.NewUnit();
            var category = TestData.NewCategory();
            db.Units.Add(unit);
            db.ComplaintCategories.Add(category);
            await db.SaveChangesAsync();

            var service = ComplaintServiceFactory.Create(db, new FakeAuthorizationService());
            // Resident is tied to a different unit than the one they're raising a complaint for.
            var actingUser = ClaimsPrincipalFactory.Resident(Guid.NewGuid(), Guid.NewGuid());
            var request = new CreateComplaintRequest { UnitId = unit.Id, ComplaintCategoryId = category.Id, Description = "Something broke" };

            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.CreateAsync(request, actingUser));
        }
    }
}
