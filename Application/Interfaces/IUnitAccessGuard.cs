using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUnitAccessGuard
    {
        // Throws NotFoundException if the unit does not exist, or ForbiddenAccessException if
        // the acting user is neither an admin nor tied to this unit.
        Task EnsureUnitAccessAsync(ClaimsPrincipal user, Guid unitId, CancellationToken ct = default);
    }
}
