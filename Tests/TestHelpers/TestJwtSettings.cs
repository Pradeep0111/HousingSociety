using Application.DTOs.Auth;

namespace Tests.TestHelpers
{
    public static class TestJwtSettings
    {
        public static JwtSettings Default(int expiryMinutes = 60) => new()
        {
            Key = "unit-test-signing-key-32-bytes-minimum!!",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiryMinutes = expiryMinutes,
        };
    }
}
