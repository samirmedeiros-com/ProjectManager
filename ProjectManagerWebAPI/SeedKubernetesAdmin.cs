using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI;

/// <summary>
/// Primeiro utilizador da Gestão Kubernetes. Sem ele não haveria como entrar: a aplicação
/// tem credenciais próprias e não aceita o login do Project Manager.
/// </summary>
public static class SeedKubernetesAdmin
{
    public static void CreateKubernetesAdminUser(ApplicationDbContext context)
    {
        const string email = "admin@kubernetes.local";
        const string password = "k8s2026";

        if (context.KubernetesUsers.FirstOrDefault(u => u.Email == email) is not null)
        {
            Console.WriteLine("✅ Utilizador Admin Kubernetes já existe");
            return;
        }

        context.KubernetesUsers.Add(new KubernetesUser
        {
            Email = email,
            FullName = "Administrador Kubernetes",
            // O mesmo SHA256 do resto do repositório, para o login funcionar da mesma forma.
            PasswordHash = KubernetesAuthService.HashPassword(password),
            Role = KubernetesAuthService.PapelAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        context.SaveChanges();

        Console.WriteLine("✅ Utilizador Admin Kubernetes criado com sucesso!");
        Console.WriteLine($"   Email: {email}");
        Console.WriteLine($"   Password: {password}  (alterar no primeiro acesso)");
    }
}
