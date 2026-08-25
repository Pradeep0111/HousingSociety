using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Authorization
{
    public class GuardTodayOnlyRequirement : IAuthorizationRequirement
    {
    }
}
