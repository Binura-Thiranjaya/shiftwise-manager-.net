using Microsoft.EntityFrameworkCore;
using TandTFuel.Api.Models;
using TandTFuel.Api.Services.Passwords;

namespace TandTFuel.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher)
    {
        await db.Database.MigrateAsync();

        // Admin
        var adminEmail = "admin@ttfuel.com";
        if (!await db.Users.AnyAsync(x => x.Email == adminEmail))
        {
            db.Users.Add(new User
            {
                Email = adminEmail,
                PasswordHash = hasher.Hash("Admin@12345"),
                Role = "admin",
                MustChangePass = true,
                IsActive = true
            });
        }

        // ShiftTypes
        var defaultTypes = new[]
        {
            ("Opening", "Morning shift - opens the station"),
            ("Closing", "Evening shift - closes the station"),
            ("Morning", "Regular morning shift"),
            ("Afternoon", "Regular afternoon shift"),
            ("Evening", "Regular evening shift"),
            ("Night", "Overnight shift"),
            ("Cleaning", "Cleaning and maintenance shift"),
            ("Training", "Training shift for new employees")
        };

        foreach (var (name, desc) in defaultTypes)
        {
            if (!await db.ShiftTypes.AnyAsync(x => x.Name == name))
            {
                db.ShiftTypes.Add(new ShiftType { Name = name, Description = desc, IsActive = true });
            }
        }

        await db.SaveChangesAsync();
    }
}