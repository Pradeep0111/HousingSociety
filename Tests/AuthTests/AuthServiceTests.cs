using Application.DTOs.Auth;
using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Tests.TestHelpers;
using Xunit;

namespace Tests.AuthTests
{
    public class AuthServiceTests
    {
        private static AuthService NewAuthService(IdentityTestContext ctx) => new(
            new UnitRepository(ctx.Db),
            new IdentityService(ctx.UserManager, ctx.RoleManager),
            new JwtTokenService(TestJwtSettings.Default()));

        private static async Task<ApplicationUser> CreateActiveUserAsync(
            IdentityTestContext ctx, Guid unitId, string email, string password, string role, bool isActive = true)
        {
            await ctx.EnsureRoleAsync(role);
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = "Test User",
                PrimaryUnitId = unitId,
                IsActive = isActive,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            var result = await ctx.UserManager.CreateAsync(user, password);
            Assert.True(result.Succeeded, string.Join(" ", result.Errors.Select(e => e.Description)));
            await ctx.UserManager.AddToRoleAsync(user, role);
            return user;
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsTokenAndUserDto()
        {
            using var ctx = IdentityTestContext.Create();
            var unit = TestData.NewUnit();
            ctx.Db.Units.Add(unit);
            await ctx.Db.SaveChangesAsync();
            var user = await CreateActiveUserAsync(ctx, unit.Id, "resident@test.local", "Passw0rd!1", "Resident");

            var service = NewAuthService(ctx);

            var response = await service.LoginAsync(new LoginRequest { Email = "resident@test.local", Password = "Passw0rd!1" });

            Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
            Assert.Equal(user.Id, response.User.Id);
            Assert.Equal(unit.Id, response.User.UnitId);
            Assert.Contains("Resident", response.User.Roles);
        }

        [Fact]
        public async Task Login_UnknownEmail_WrongPassword_AndInactiveAccount_AllThrowTheSameMessage()
        {
            // Real security property: these three failure paths must be indistinguishable to
            // the caller, or the response becomes an account-enumeration oracle.
            using var ctx = IdentityTestContext.Create();
            var unit = TestData.NewUnit();
            ctx.Db.Units.Add(unit);
            await ctx.Db.SaveChangesAsync();
            await CreateActiveUserAsync(ctx, unit.Id, "known@test.local", "Correct#Pass1", "Resident");
            await CreateActiveUserAsync(ctx, unit.Id, "inactive@test.local", "Correct#Pass1", "Resident", isActive: false);

            var service = NewAuthService(ctx);

            var unknownEmail = await Assert.ThrowsAsync<AuthenticationFailedException>(
                () => service.LoginAsync(new LoginRequest { Email = "nobody@test.local", Password = "whatever" }));
            var wrongPassword = await Assert.ThrowsAsync<AuthenticationFailedException>(
                () => service.LoginAsync(new LoginRequest { Email = "known@test.local", Password = "wrong-password" }));
            var inactiveAccount = await Assert.ThrowsAsync<AuthenticationFailedException>(
                () => service.LoginAsync(new LoginRequest { Email = "inactive@test.local", Password = "Correct#Pass1" }));

            Assert.Equal(unknownEmail.Message, wrongPassword.Message);
            Assert.Equal(unknownEmail.Message, inactiveAccount.Message);
        }

        [Fact]
        public async Task Register_UnknownUnit_ThrowsNotFound()
        {
            using var ctx = IdentityTestContext.Create();
            await ctx.EnsureRoleAsync("Resident");
            var service = NewAuthService(ctx);

            var request = new RegisterRequest { Email = "new@test.local", Password = "Passw0rd!1", FullName = "New User", PrimaryUnitId = Guid.NewGuid(), Role = "Resident" };

            await Assert.ThrowsAsync<NotFoundException>(() => service.RegisterAsync(request));
        }

        [Fact]
        public async Task Register_UnknownRole_ThrowsValidation()
        {
            using var ctx = IdentityTestContext.Create();
            var unit = TestData.NewUnit();
            ctx.Db.Units.Add(unit);
            await ctx.Db.SaveChangesAsync();
            var service = NewAuthService(ctx);

            var request = new RegisterRequest { Email = "new@test.local", Password = "Passw0rd!1", FullName = "New User", PrimaryUnitId = unit.Id, Role = "NotARealRole" };

            await Assert.ThrowsAsync<ValidationException>(() => service.RegisterAsync(request));
        }

        [Fact]
        public async Task Register_WeakPassword_ThrowsValidation()
        {
            using var ctx = IdentityTestContext.Create();
            var unit = TestData.NewUnit();
            ctx.Db.Units.Add(unit);
            await ctx.Db.SaveChangesAsync();
            await ctx.EnsureRoleAsync("Resident");
            var service = NewAuthService(ctx);

            var request = new RegisterRequest { Email = "new@test.local", Password = "weak", FullName = "New User", PrimaryUnitId = unit.Id, Role = "Resident" };

            await Assert.ThrowsAsync<ValidationException>(() => service.RegisterAsync(request));
        }

        [Fact]
        public async Task Register_Success_CreatesActiveUserAndAddsRole()
        {
            using var ctx = IdentityTestContext.Create();
            var unit = TestData.NewUnit();
            ctx.Db.Units.Add(unit);
            await ctx.Db.SaveChangesAsync();
            await ctx.EnsureRoleAsync("Resident");
            var service = NewAuthService(ctx);

            var request = new RegisterRequest { Email = "new@test.local", Password = "Passw0rd!1", FullName = "New User", PrimaryUnitId = unit.Id, Role = "Resident" };

            var dto = await service.RegisterAsync(request);

            Assert.Equal(unit.Id, dto.UnitId);
            Assert.Contains("Resident", dto.Roles);
            var stored = await ctx.UserManager.FindByEmailAsync("new@test.local");
            Assert.NotNull(stored);
            Assert.True(stored!.IsActive);
        }

        [Fact]
        public async Task GetCurrentUser_ReturnsOwnRecord()
        {
            using var ctx = IdentityTestContext.Create();
            var unit = TestData.NewUnit();
            ctx.Db.Units.Add(unit);
            await ctx.Db.SaveChangesAsync();
            var user = await CreateActiveUserAsync(ctx, unit.Id, "self@test.local", "Passw0rd!1", "Resident");
            var service = NewAuthService(ctx);
            var principal = ClaimsPrincipalFactory.WithoutUnit(user.Id);

            var dto = await service.GetCurrentUserAsync(principal);

            Assert.Equal(user.Id, dto.Id);
            Assert.Equal(unit.Id, dto.UnitId);
        }

        [Fact]
        public async Task GetCurrentUser_UnknownUser_ThrowsNotFound()
        {
            using var ctx = IdentityTestContext.Create();
            var service = NewAuthService(ctx);
            var principal = ClaimsPrincipalFactory.WithoutUnit(Guid.NewGuid());

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetCurrentUserAsync(principal));
        }
    }
}
