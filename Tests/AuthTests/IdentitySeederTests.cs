using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Tests.AuthTests
{
    public class IdentitySeederTests
    {
        // A fixture-owned, fabricated credential used only to exercise the seeder's own config
        // read path — never the real application's bootstrap-admin secret.
        private const string FixtureBootstrapPassword = "Fixture-Bootstrap#Pw1";

        private sealed class FakeEnvironment(string name) : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = name;
            public string ApplicationName { get; set; } = "Tests";
            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        }

        private static ServiceProvider BuildServices(out AppDbContext db)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            var provider = services.BuildServiceProvider();
            db = provider.GetRequiredService<AppDbContext>();
            return provider;
        }

        private static IConfiguration ConfigWith(string? email, string? password) =>
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BootstrapAdmin:Email"] = email,
                ["BootstrapAdmin:Password"] = password,
                ["BootstrapAdmin:FullName"] = "Fixture Admin",
            }).Build();

        [Fact]
        public async Task SeedAsync_CreatesTheFourRoles()
        {
            using var provider = BuildServices(out _);
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            await IdentitySeeder.SeedAsync(provider, ConfigWith(null, null), new FakeEnvironment("Production"));

            foreach (var role in new[] { "Secretary", "CommitteeMember", "Resident", "Guard" })
                Assert.True(await roleManager.RoleExistsAsync(role), $"expected role '{role}' to exist");
        }

        [Fact]
        public async Task SeedAsync_IsIdempotentOnSecondRun()
        {
            using var provider = BuildServices(out _);
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var config = ConfigWith(null, null);
            var env = new FakeEnvironment("Production");

            await IdentitySeeder.SeedAsync(provider, config, env);
            await IdentitySeeder.SeedAsync(provider, config, env); // must not throw or duplicate

            var roles = roleManager.Roles.ToList();
            Assert.Equal(4, roles.Count);
        }

        [Fact]
        public async Task SeedAsync_Development_WithConfig_CreatesBootstrapAdminInSecretaryRole()
        {
            using var provider = BuildServices(out _);
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var config = ConfigWith("fixture-admin@test.local", FixtureBootstrapPassword);

            await IdentitySeeder.SeedAsync(provider, config, new FakeEnvironment("Development"));

            var admin = await userManager.FindByEmailAsync("fixture-admin@test.local");
            Assert.NotNull(admin);
            Assert.True(admin!.IsActive);
            Assert.Contains("Secretary", await userManager.GetRolesAsync(admin));
        }

        [Fact]
        public async Task SeedAsync_NonDevelopment_SkipsBootstrapAdminEvenWithConfig()
        {
            using var provider = BuildServices(out _);
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var config = ConfigWith("fixture-admin@test.local", FixtureBootstrapPassword);

            await IdentitySeeder.SeedAsync(provider, config, new FakeEnvironment("Production"));

            Assert.Null(await userManager.FindByEmailAsync("fixture-admin@test.local"));
        }

        [Fact]
        public async Task SeedAsync_Development_MissingBootstrapConfig_SkipsBootstrapAdmin()
        {
            using var provider = BuildServices(out _);
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var config = ConfigWith(null, null);

            await IdentitySeeder.SeedAsync(provider, config, new FakeEnvironment("Development"));

            // No email configured, so nothing to look up by — assert the store stayed empty instead.
            Assert.Empty(userManager.Users);
        }

        [Fact]
        public async Task SeedAsync_Development_EmailAlreadyExists_SkipsCreatingBootstrapAdmin()
        {
            using var provider = BuildServices(out _);
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var existing = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "fixture-admin@test.local",
                Email = "fixture-admin@test.local",
                FullName = "Already Here",
                PrimaryUnitId = Guid.NewGuid(),
                IsActive = true,
            };
            Assert.True((await userManager.CreateAsync(existing, "Existing#Pw1")).Succeeded);
            var config = ConfigWith("fixture-admin@test.local", FixtureBootstrapPassword);

            await IdentitySeeder.SeedAsync(provider, config, new FakeEnvironment("Development"));

            // Still exactly the pre-existing user — the seeder did not overwrite or duplicate it.
            Assert.Single(userManager.Users);
            var stored = await userManager.FindByEmailAsync("fixture-admin@test.local");
            Assert.Equal("Already Here", stored!.FullName);
        }
    }
}
