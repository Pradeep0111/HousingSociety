using Api.Controllers;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tests.AuthTests
{
    /// Boots the real Api pipeline (Program.cs, real auth middleware, real controllers) against
    /// an in-memory database instead of the real SQL Server, so integration tests exercise
    /// exactly the layer where the HS-6 cookie-vs-Bearer defect lived. WebApplicationFactory's
    /// generic parameter just needs to be any public type from the Api assembly — it does not
    /// have to be the (internal, top-level-statement-generated) Program class, so no edit to
    /// Api/Program.cs is needed to make this work.
    public class ApiFactory : WebApplicationFactory<AuthController>
    {
        public const string BootstrapAdminEmail = "fixture-admin@test.local";
        // Fixture-owned, fabricated credential read via config the same way the real seeder
        // reads BootstrapAdmin:Password -- never the real application's secret.
        public const string BootstrapAdminPassword = "Fixture-Bootstrap#Pw1";
        public const string JwtKey = "integration-test-signing-key-32-bytes-min!";
        public const string JwtIssuer = "integration-test-issuer";
        public const string JwtAudience = "integration-test-audience";

        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = JwtKey,
                    ["Jwt:Issuer"] = JwtIssuer,
                    ["Jwt:Audience"] = JwtAudience,
                    ["Jwt:ExpiryMinutes"] = "60",
                    ["ConnectionStrings:DefaultConnection"] = "Server=(unused);Database=unused;TrustServerCertificate=True;",
                    ["BootstrapAdmin:Email"] = BootstrapAdminEmail,
                    ["BootstrapAdmin:Password"] = BootstrapAdminPassword,
                    ["BootstrapAdmin:FullName"] = "Fixture Admin",
                });
            });

            builder.ConfigureServices(services =>
            {
                // Removing only DbContextOptions<AppDbContext> is not enough: AddDbContext also
                // registers an IDbContextOptionsConfiguration<AppDbContext> entry, and a second
                // AddDbContext call appends to that list rather than replacing it, so both
                // UseSqlServer (Program.cs) and UseInMemoryDatabase (here) end up applied to the
                // same options -- hence EF's "single database provider" error unless this is
                // removed too.
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
            });
        }
    }
}
