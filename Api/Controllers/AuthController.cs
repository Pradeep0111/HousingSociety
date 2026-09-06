using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Auth;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    // No class-level [Authorize] here (unlike ComplaintsController) — login must stay
    // reachable by anonymous callers, so each action states its own requirement instead.
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.CreateToken(user, roles);
            return Ok(new AuthResponse { Token = token.AccessToken, ExpiresAt = token.ExpiresAt });
        }

        // Residents are onboarded by the committee, not self-signup — an open endpoint here
        // would let anyone mint themselves a unitId claim and read another unit's complaints.
        [HttpPost("register")]
        [Authorize(Roles = "Secretary")]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                PrimaryUnitId = request.PrimaryUnitId
            };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);
            await _userManager.AddToRoleAsync(user, request.Role);
            return CreatedAtAction(nameof(Register), new { id = user.Id }, new { user.Id });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                UnitId = user.PrimaryUnitId,
                Roles = roles.ToList(),
            });
        }
    }
}
