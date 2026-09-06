using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Application.DTOs.Auth;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Tests.TestHelpers;
using Xunit;

namespace Tests.AuthTests
{
    /// Exercises the real ASP.NET Core middleware pipeline over HTTP -- this is the layer where
    /// the HS-6 cookie-vs-Bearer defect lived, and where the unit tests elsewhere in this suite
    /// are blind. A clean build and a green unit-test suite were both insufficient to catch that
    /// defect; only a real request through the real pipeline caught it.
    public class AuthIntegrationTests : IClassFixture<ApiFactory>
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly ApiFactory _factory;

        public AuthIntegrationTests(ApiFactory factory) => _factory = factory;

        private static HttpClient NoRedirectClient(ApiFactory factory) =>
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        private async Task<string> LoginAsync(HttpClient client, string email, string password)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password }, JsonOptions);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
            return body!.Token;
        }

        [Fact]
        public async Task ValidToken_ReachesAuthorizeEndpoint_Returns200()
        {
            var client = _factory.CreateClient();
            var token = await LoginAsync(client, ApiFactory.BootstrapAdminEmail, ApiFactory.BootstrapAdminPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/complaints");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task NoToken_Returns401_WithBearerChallenge_AndNoLoginRedirect()
        {
            var client = NoRedirectClient(_factory);

            var response = await client.GetAsync("/api/complaints");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains(response.Headers.WwwAuthenticate, h => h.Scheme == "Bearer");
            // HS-6 regression guard: the Identity cookie handler must never issue this challenge.
            Assert.False(response.Headers.Contains("Location"), "response must not carry a cookie-login redirect");
        }

        [Fact]
        public async Task GarbageToken_Returns401()
        {
            var client = NoRedirectClient(_factory);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

            var response = await client.GetAsync("/api/complaints");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ExpiredToken_Returns401()
        {
            var client = NoRedirectClient(_factory);
            // JwtTokenService always mints notBefore=now, so a negative ExpiryMinutes puts
            // Expires before NotBefore and the JWT library itself rejects construction. Build the
            // token by hand instead, with both timestamps in the past but correctly ordered, so
            // it is a well-formed, correctly-signed token that is simply expired.
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactory.JwtKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;
            var expiredJwt = new JwtSecurityToken(
                issuer: ApiFactory.JwtIssuer,
                audience: ApiFactory.JwtAudience,
                claims: [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                notBefore: now.AddHours(-2),
                expires: now.AddHours(-1),
                signingCredentials: credentials);
            var expiredToken = new JwtSecurityTokenHandler().WriteToken(expiredJwt);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

            var response = await client.GetAsync("/api/complaints");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RegisterAsResident_Returns403_NotOk()
        {
            var client = _factory.CreateClient();
            var adminToken = await LoginAsync(client, ApiFactory.BootstrapAdminEmail, ApiFactory.BootstrapAdminPassword);

            Guid unitId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var unit = TestData.NewUnit();
                db.Units.Add(unit);
                await db.SaveChangesAsync();
                unitId = unit.Id;
            }

            // Create a real Resident (as the admin), then log in as them.
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
            {
                email = "resident-under-test@test.local",
                password = "Passw0rd!1",
                fullName = "Resident Under Test",
                primaryUnitId = unitId,
                role = "Resident",
            }, JsonOptions);
            registerResponse.EnsureSuccessStatusCode();

            var residentToken = await LoginAsync(client, "resident-under-test@test.local", "Passw0rd!1");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", residentToken);
            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                email = "should-not-be-created@test.local",
                password = "Passw0rd!1",
                fullName = "Should Not Exist",
                primaryUnitId = unitId,
                role = "Resident",
            }, JsonOptions);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
