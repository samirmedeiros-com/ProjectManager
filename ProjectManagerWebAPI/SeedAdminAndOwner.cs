using System.Security.Cryptography;
using System.Text;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI;

public static class SeedAdminAndOwner
{
    public static void CreateAdminAndOwnerUsers(ApplicationDbContext context)
    {
        // Criar utilizador Owner
        CreateOwnerUser(context);

        // Criar utilizador Admin
        CreateAdminUser(context);
    }

    private static void CreateOwnerUser(ApplicationDbContext context)
    {
        if (context.Users.Any(u => u.Email == "owner@example.com"))
        {
            Console.WriteLine("✅ Utilizador Owner já existe");
            return;
        }

        var passwordHash = HashPassword("owner123");
        var ownerUser = new User
        {
            Email = "owner@example.com",
            FullName = "Proprietário Sistema",
            PasswordHash = passwordHash,
            Department = "Administração",
            Role = "Owner",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(ownerUser);
        context.SaveChanges();

        Console.WriteLine("✅ Utilizador Owner criado com sucesso!");
        Console.WriteLine("   Email: owner@example.com");
        Console.WriteLine("   Password: owner123");
    }

    private static void CreateAdminUser(ApplicationDbContext context)
    {
        if (context.Users.Any(u => u.Email == "admin@example.com"))
        {
            Console.WriteLine("✅ Utilizador Admin já existe");
            return;
        }

        var passwordHash = HashPassword("admin123");
        var adminUser = new User
        {
            Email = "admin@example.com",
            FullName = "Administrador Sistema",
            PasswordHash = passwordHash,
            Department = "Administração",
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        context.SaveChanges();

        Console.WriteLine("✅ Utilizador Admin criado com sucesso!");
        Console.WriteLine("   Email: admin@example.com");
        Console.WriteLine("   Password: admin123");
    }

    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
