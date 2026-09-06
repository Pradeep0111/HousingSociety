using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IIdentityService
    {
        Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken ct = default);

        Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken ct = default);

        Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user, CancellationToken ct = default);

        Task<IdentityOperationResult> CreateUserAsync(ApplicationUser user, string password, CancellationToken ct = default);

        Task AddToRoleAsync(ApplicationUser user, string role, CancellationToken ct = default);

        Task<bool> RoleExistsAsync(string role, CancellationToken ct = default);

        Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal, CancellationToken ct = default);
    }
}
