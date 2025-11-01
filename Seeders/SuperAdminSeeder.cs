using InvoiceService.Data;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;


namespace InvoiceService.Seeders;

public static class SuperAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var superAdminEmail = "superadmin@invoicepro.com";
        var exists = await context.Users.AnyAsync(u => u.Email == superAdminEmail);

        if (exists)
        {
        //    Console.WriteLine("Super admin already exists");
            return;
        }

        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var passwordText = config["AdminSettings:superAdminPassword"];
        if (string.IsNullOrEmpty(passwordText))
        {
        //    Console.WriteLine("WARNING: AdminSettings:superAdminPassword not set. Skipping super admin creation.");
            return;
        }

        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            Email = superAdminEmail,
            FullName = "Super Admin",
            PhoneNumber = "",
            Password =  BCrypt.Net.BCrypt.HashPassword(passwordText),
            Role = "super_admin",
            CreatedAt = DateTime.UtcNow,
        };

        context.Users.Add(superAdmin);
        await context.SaveChangesAsync();
    }
}