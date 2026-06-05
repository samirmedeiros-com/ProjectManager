using System.Security.Cryptography;
using System.Text;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI;

public static class SeedAdminAndOwner
{
    public static void CreateAdminAndOwnerUsers(ApplicationDbContext context)
    {
        // Criar utilizador Admin para produção
        CreateAdminUser(context);
    }

    private static void CreateAdminUser(ApplicationDbContext context)
    {
        // Usar FirstOrDefault ao invés de Any para evitar CASE WHEN com TRUE/FALSE
        var existingAdmin = context.Users.FirstOrDefault(u => u.Email == "admin@admin.local");
        if (existingAdmin != null)
        {
            Console.WriteLine("✅ Utilizador Admin já existe");
            return;
        }

        var passwordHash = HashPassword("nsam150123");
        var adminUser = new User
        {
            Email = "admin@admin.local",
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
        Console.WriteLine("   Email: admin@admin.local");
        Console.WriteLine("   Password: nsam150123");
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
