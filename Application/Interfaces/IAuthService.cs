using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Auth;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

        Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

        Task<UserDto> GetCurrentUserAsync(ClaimsPrincipal actingUser, CancellationToken ct = default);
    }
}
