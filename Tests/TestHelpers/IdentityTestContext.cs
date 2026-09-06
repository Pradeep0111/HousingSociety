using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.TestHelpers
{
    /// Wires a real UserManager/RoleManager backed by an isolated in-memory AppDbContext,
    /// mirroring the exact Identity configuration in Api/Program.cs (RequiredLength=8,
    /// RequireUniqueEmail=true, everything else left at ASP.NET Core Identity's defaults), so
    /// AuthService tests exercise genuine password hashing/validation instead of a hand-rolled
    /// fake UserManager.
    public sealed class IdentityTestContext : IDisposable
    {
        public AppDbContext Db { get; }
        public UserManager<ApplicationUser> UserManager { get; }
        public RoleManager<IdentityRole<Guid>> RoleManager { get; }
        private readonly ServiceProvider _provider;

        private IdentityTestContext(AppDbContext db, ServiceProvider provider)
        {
            Db = db;
            _provider = provider;
            UserManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            RoleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        }

        public static IdentityTestContext Create()
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
            var db = provider.GetRequiredService<AppDbContext>();
            return new IdentityTestContext(db, provider);
        }

        public async Task EnsureRoleAsync(string role)
        {
            if (!await RoleManager.RoleExistsAsync(role))
                await RoleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        public void Dispose() => _provider.Dispose();
    }
}
