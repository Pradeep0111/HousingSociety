using System.Security.Claims;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Tests.TestHelpers
{
    /// A controllable stand-in for IAuthorizationService that mirrors the real
    /// Infrastructure.Authorization.SameUnitOrAdminHandler exactly (admin role bypasses,
    /// otherwise the caller's "unitId" claim must match the resource Unit's Id). This lets
    /// ComplaintService tests exercise the same access rule the production handler enforces
    /// without wiring up the full ASP.NET Core authorization pipeline.
    public class FakeAuthorizationService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => throw new NotSupportedException("ComplaintService only calls the policy-name overload.");

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
        {
            if (policyName != "SameUnitorAdmin")
                throw new NotSupportedException($"Unexpected policy '{policyName}'.");

            var isAdmin = user.IsInRole("Secretary") || user.IsInRole("CommitteeMember");
            var userUnitId = user.FindFirstValue("unitId");
            var succeeded = isAdmin || (resource is Unit unit && userUnitId == unit.Id.ToString());

            return Task.FromResult(succeeded ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        }
    }
}
