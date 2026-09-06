using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Auth;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitRepository _unitRepository;
        private readonly IIdentityService _identityService;
        private readonly IJwtTokenService _tokenService;

        public AuthService(
            IUnitRepository unitRepository,
            IIdentityService identityService,
            IJwtTokenService tokenService)
        {
            _unitRepository = unitRepository;
            _identityService = identityService;
            _tokenService = tokenService;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var user = await _identityService.FindByEmailAsync(request.Email, ct);

            // Single generic failure for "no such user", "wrong password" and "inactive
            // account" alike — never let the response reveal which one it was.
            if (user is null || !user.IsActive || !await _identityService.CheckPasswordAsync(user, request.Password, ct))
                throw new AuthenticationFailedException("Invalid email or password.");

            var roles = await _identityService.GetRolesAsync(user, ct);
            var token = _tokenService.CreateToken(user, roles);

            return new LoginResponse { AccessToken = token.AccessToken, ExpiresAt = token.ExpiresAt, User = ToUserDto(user, roles) };
        }

        public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            if (request.PrimaryUnitId is { } primaryUnitId && !await _unitRepository.ExistsAsync(primaryUnitId, ct))
                throw new NotFoundException($"Unit '{primaryUnitId}' was not found.");

            if (!await _identityService.RoleExistsAsync(request.Role, ct))
                throw new ValidationException($"Role '{request.Role}' does not exist.");

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                PrimaryUnitId = request.PrimaryUnitId,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var createResult = await _identityService.CreateUserAsync(user, request.Password, ct);
            if (!createResult.Succeeded)
                throw new ValidationException(string.Join(" ", createResult.Errors));

            await _identityService.AddToRoleAsync(user, request.Role, ct);

            return ToUserDto(user, new[] { request.Role });
        }

        public async Task<UserDto> GetCurrentUserAsync(ClaimsPrincipal actingUser, CancellationToken ct = default)
        {
            var user = await _identityService.GetUserAsync(actingUser, ct)
                ?? throw new NotFoundException("The current user could not be found.");
            var roles = await _identityService.GetRolesAsync(user, ct);

            return ToUserDto(user, roles);
        }

        // Email is always set by our own user-creation paths (login/register), so this is safe.
        private static UserDto ToUserDto(ApplicationUser user, IReadOnlyList<string> roles) => new()
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            UnitId = user.PrimaryUnitId,
            Roles = roles.ToList(),
        };
    }
}
