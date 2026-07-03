using System.Security.Cryptography;
using System.Text;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI;

public static class SeedSeurAdmin
{
    public static void CreateSeurAdminUser(ApplicationDbContext context)
    {
        var existing = context.SeurUsers.FirstOrDefault(u => u.Email == "admin@seur.local");
        if (existing != null)
        {
            Console.WriteLine("✅ Utilizador Admin SEUR já existe");
            return;
        }

        var seurAdmin = new SeurUser
        {
            Email = "admin@seur.local",
            FullName = "Administrador SEUR",
            PasswordHash = HashPassword("seur2026"),
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.SeurUsers.Add(seurAdmin);
        context.SaveChanges();

        Console.WriteLine("✅ Utilizador Admin SEUR criado com sucesso!");
        Console.WriteLine("   Email: admin@seur.local");
        Console.WriteLine("   Password: seur2026");
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
