using System;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Seeding
{
    // Runtime data seeding only — no migration, no new entities.
    public static class IdentitySeeder
    {
        private static readonly string[] Roles = ["Secretary", "CommitteeMember", "Resident", "Guard"];

        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }

            // The bootstrap admin only ever exists in Development, so there is a way to mint
            // the first Secretary token. This account must never be created in production.
            if (!environment.IsDevelopment())
                return;

            var email = configuration["BootstrapAdmin:Email"];
            var password = configuration["BootstrapAdmin:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return;

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            if (await userManager.FindByEmailAsync(email) is not null)
                return;

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = configuration["BootstrapAdmin:FullName"] ?? "Bootstrap Admin",
                // Secretary bypasses the same-unit check entirely (SameUnitOrAdminHandler), so no
                // unit is needed here.
                PrimaryUnitId = null,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var result = await userManager.CreateAsync(admin, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Secretary");
        }
    }
}
