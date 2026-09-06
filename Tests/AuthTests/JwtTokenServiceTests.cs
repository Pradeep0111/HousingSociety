using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Entities;
using Infrastructure.Services;
using Tests.TestHelpers;
using Xunit;

namespace Tests.AuthTests
{
    public class JwtTokenServiceTests
    {
        private static ApplicationUser NewUser() => new()
        {
            Id = Guid.NewGuid(),
            UserName = "resident@test.local",
            Email = "resident@test.local",
            FullName = "Resident One",
            PrimaryUnitId = Guid.NewGuid(),
            IsActive = true,
        };

        [Fact]
        public void CreateToken_IncludesExactClaimSet()
        {
            var settings = TestJwtSettings.Default();
            var service = new JwtTokenService(settings);
            var user = NewUser();

            var token = service.CreateToken(user, new[] { "Resident" });
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

            Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
            Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
            Assert.Single(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
            Assert.Single(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Iat);
            // Literal claim type name, not a constant reference: SameUnitOrAdminHandler and
            // ComplaintService.ListAsync both read the string "unitId" directly, so a rename of
            // whatever constant JwtTokenService might switch to must still fail this test.
            Assert.Equal(user.PrimaryUnitId.ToString(), jwt.Claims.Single(c => c.Type == "unitId").Value);
            Assert.Equal("Resident", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);

            Assert.Equal(settings.Issuer, jwt.Issuer);
            Assert.Equal(settings.Audience, Assert.Single(jwt.Audiences));
        }

        [Fact]
        public void CreateToken_EmitsOneRoleClaimPerRole()
        {
            var service = new JwtTokenService(TestJwtSettings.Default());
            var user = NewUser();

            var token = service.CreateToken(user, new[] { "Secretary", "CommitteeMember" });
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

            var roleValues = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
            Assert.Equal(new[] { "Secretary", "CommitteeMember" }, roleValues);
        }

        [Fact]
        public void CreateToken_NoRoles_EmitsNoRoleClaims()
        {
            var service = new JwtTokenService(TestJwtSettings.Default());
            var user = NewUser();

            var token = service.CreateToken(user, Array.Empty<string>());
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

            Assert.DoesNotContain(jwt.Claims, c => c.Type == ClaimTypes.Role);
        }

        [Fact]
        public void CreateToken_ExpiryHonoursExpiryMinutes()
        {
            var settings = TestJwtSettings.Default(expiryMinutes: 15);
            var service = new JwtTokenService(settings);
            var user = NewUser();

            var before = DateTimeOffset.UtcNow;
            var token = service.CreateToken(user, Array.Empty<string>());
            var after = DateTimeOffset.UtcNow;

            Assert.InRange(token.ExpiresAt, before.AddMinutes(15).AddSeconds(-2), after.AddMinutes(15).AddSeconds(2));

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);
            Assert.True(Math.Abs((jwt.ValidTo - token.ExpiresAt.UtcDateTime).TotalSeconds) < 1);
        }
    }
}
