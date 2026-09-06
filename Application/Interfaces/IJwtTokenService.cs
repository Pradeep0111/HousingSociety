using System.Collections.Generic;
using Application.DTOs.Auth;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IJwtTokenService
    {
        JwtToken CreateToken(ApplicationUser user, IEnumerable<string> roles);
    }
}
