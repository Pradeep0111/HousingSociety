using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Authorization
{
    public class GuardTodayOnlyHandler : AuthorizationHandler<GuardTodayOnlyRequirement, VisitorEntry>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GuardTodayOnlyRequirement requirement, VisitorEntry resource)
        {
            var guardId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isOwnEntity = resource.LoggedbyGuardUserId.ToString() == guardId;
            var isToday = resource.EntryTime.Date == DateTimeOffset.UtcNow.Date;

            if (isOwnEntity && isToday)
                context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
