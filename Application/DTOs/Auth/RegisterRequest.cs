using System;

namespace Application.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Guid? PrimaryUnitId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
