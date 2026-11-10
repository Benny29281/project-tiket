using HelpDeskKu.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HelpDeskKu.Infrastructure;

public static class DbInitializer
{
    public static async Task Initialize(AppDbContext db, UserManager<AppUser> users, RoleManager<IdentityRole> roles, IConfiguration config)
    {
        // Explicit opt-in for a fresh local database. Production uses reviewed migrations.
        if (config.GetValue<bool>("Database:Initialize")) await db.Database.EnsureCreatedAsync();
        foreach (var role in new[] { "Requester", "Agent", "Admin" })
            if (!await roles.RoleExistsAsync(role)) Require(await roles.CreateAsync(new IdentityRole(role)));
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(new Category { Name = "Hardware", Description = "Laptop, printer, dan perangkat kerja" },
                new Category { Name = "Software", Description = "Aplikasi dan instalasi" },
                new Category { Name = "Jaringan", Description = "Internet, Wi-Fi, dan VPN" },
                new Category { Name = "Akun", Description = "Akses dan akun pengguna" });
        }
        if (!await db.Priorities.AnyAsync()) db.Priorities.AddRange(
            new Priority { Name = "Low", Level = 1, ResponseTimeMinutes = 1440, ResolutionTimeMinutes = 10080 },
            new Priority { Name = "Medium", Level = 2, ResponseTimeMinutes = 480, ResolutionTimeMinutes = 4320 },
            new Priority { Name = "High", Level = 3, ResponseTimeMinutes = 120, ResolutionTimeMinutes = 480 },
            new Priority { Name = "Critical", Level = 4, ResponseTimeMinutes = 30, ResolutionTimeMinutes = 240 });
        await db.SaveChangesAsync();
        var email = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password) && await users.FindByEmailAsync(email) == null)
        {
            var admin = new AppUser { UserName = email, Email = email, FullName = "Administrator" };
            Require(await users.CreateAsync(admin, password));
            Require(await users.AddToRoleAsync(admin, "Admin"));
        }
    }
    private static void Require(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
