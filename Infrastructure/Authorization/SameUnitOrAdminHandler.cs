using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Infrastructure.Authorization
{
    public class SameUnitOrAdminHandler : AuthorizationHandler<SameUnitOrAdminRequirement, Unit>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SameUnitOrAdminRequirement requirement, Unit resource)
        {
            var isAdmin = context.User.IsInRole("Secretary") || context.User.IsInRole("CommitteeMember");
            var userUnitId = context.User.FindFirstValue("unitId");

            if(isAdmin || (userUnitId == resource.Id.ToString()))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}