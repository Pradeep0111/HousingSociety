using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authorization
{
    public class UnitAccessGuard : IUnitAccessGuard
    {
        private readonly AppDbContext _db;
        private readonly IAuthorizationService _authorizationService;

        public UnitAccessGuard(AppDbContext db, IAuthorizationService authorizationService)
        {
            _db = db;
            _authorizationService = authorizationService;
        }

        public async Task EnsureUnitAccessAsync(ClaimsPrincipal user, Guid unitId, CancellationToken ct = default)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == unitId, ct)
                ?? throw new NotFoundException($"Unit '{unitId}' was not found.");

            var result = await _authorizationService.AuthorizeAsync(user, unit, "SameUnitorAdmin");
            if (!result.Succeeded)
                throw new ForbiddenAccessException("You do not have access to this unit's complaints.");
        }
    }
}
