using System.Security.Cryptography;
using System.Text;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI;

public static class SeedGestorUser
{
    public static void CreateGestorUser(ApplicationDbContext context)
    {
        // Verificar se o utilizador já existe (usar FirstOrDefault ao invés de Any)
        var existingGestor = context.Users.FirstOrDefault(u => u.Email == "gestor@example.com");
        if (existingGestor != null)
        {
            Console.WriteLine("✅ Utilizador Gestor já existe");
            return;
        }

        // Criar o utilizador Gestor
        var passwordHash = HashPassword("gestor123");
        var gestorUser = new User
        {
            Email = "gestor@example.com",
            FullName = "João Gestor",
            PasswordHash = passwordHash,
            Department = "Gestão de Projetos",
            Role = "Gestor",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(gestorUser);
        context.SaveChanges();

        Console.WriteLine("✅ Utilizador Gestor criado com sucesso!");
        Console.WriteLine("   Email: gestor@example.com");
        Console.WriteLine("   Senha: gestor123");
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
