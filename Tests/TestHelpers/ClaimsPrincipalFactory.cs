using System.Security.Claims;

namespace Tests.TestHelpers
{
    public static class ClaimsPrincipalFactory
    {
        /// A resident: has a NameIdentifier and a "unitId" claim tying them to one unit.
        public static ClaimsPrincipal Resident(Guid userId, Guid unitId)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("unitId", unitId.ToString()),
            }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        /// An admin: has a NameIdentifier and a Secretary/CommitteeMember role, no unitId claim.
        public static ClaimsPrincipal Admin(Guid userId, string role = "Secretary")
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
            }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        /// A user with a NameIdentifier but no unitId claim and no admin role.
        public static ClaimsPrincipal WithoutUnit(Guid userId)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }
    }
}
