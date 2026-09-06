using System;

namespace Application.DTOs.Auth
{
    public class JwtToken
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
