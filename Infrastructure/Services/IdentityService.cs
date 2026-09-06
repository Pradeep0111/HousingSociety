using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public IdentityService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            await _userManager.FindByEmailAsync(email);

        public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken ct = default) =>
            await _userManager.CheckPasswordAsync(user, password);

        public async Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user, CancellationToken ct = default) =>
            (await _userManager.GetRolesAsync(user)).ToList();

        public async Task<IdentityOperationResult> CreateUserAsync(ApplicationUser user, string password, CancellationToken ct = default)
        {
            var result = await _userManager.CreateAsync(user, password);
            return new IdentityOperationResult
            {
                Succeeded = result.Succeeded,
                Errors = result.Errors.Select(e => e.Description).ToList(),
            };
        }

        public async Task AddToRoleAsync(ApplicationUser user, string role, CancellationToken ct = default) =>
            await _userManager.AddToRoleAsync(user, role);

        public async Task<bool> RoleExistsAsync(string role, CancellationToken ct = default) =>
            await _roleManager.RoleExistsAsync(role);

        public async Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal, CancellationToken ct = default) =>
            await _userManager.GetUserAsync(principal);
    }
}
