using System.Security.Cryptography;
using System.Text;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI;

public static class SeedTestData
{
    public static void CreateTestData(ApplicationDbContext context)
    {
        // Criar utilizadores de teste se não existirem
        CreateTestUsers(context);

        // Criar projetos de teste
        CreateTestProjects(context);
    }

    private static void CreateTestUsers(ApplicationDbContext context)
    {
        var users = new List<(string email, string name, string password, string role)>
        {
            ("ana@example.com", "Ana Silva", "senha123", "Utilizador"),
            ("carlos@example.com", "Carlos Santos", "senha123", "Utilizador"),
            ("diana@example.com", "Diana Costa", "senha123", "Utilizador"),
            ("eu@example.com", "Eu Desenvolvedor", "senha123", "Utilizador")
        };

        foreach (var (email, name, password, role) in users)
        {
            // Usar FirstOrDefault ao invés de Any para evitar CASE WHEN com TRUE/FALSE
            if (context.Users.FirstOrDefault(u => u.Email == email) == null)
            {
                var passwordHash = HashPassword(password);
                var user = new User
                {
                    Email = email,
                    FullName = name,
                    PasswordHash = passwordHash,
                    Department = role == "Gestor" ? "Gestão" : "Desenvolvimento",
                    Role = role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(user);
                Console.WriteLine($"✅ Utilizador criado: {name} ({email})");
            }
        }

        context.SaveChanges();
    }

    private static void CreateTestProjects(ApplicationDbContext context)
    {
        var projects = new List<(string name, string description, string manager, DateTime startDate, DateTime? endDate, int priority)>
        {
            ("Website Redesign", "Redesenhar o website da empresa com novo design", "gestor@example.com", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(20), 3),
            ("Mobile App", "Desenvolver aplicação móvel para iOS e Android", "gestor@example.com", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(60), 5),
            ("API Integration", "Integrar com API de terceiros", "gestor@example.com", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 2),
            ("Database Migration", "Migrar dados para novo banco de dados", "gestor@example.com", DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(10), 4),
            ("Security Audit", "Auditoria de segurança do sistema", "gestor@example.com", DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(15), 5)
        };

        foreach (var (name, description, manager, startDate, endDate, priority) in projects)
        {
            // Usar FirstOrDefault ao invés de Any para evitar CASE WHEN com TRUE/FALSE
            if (context.Projects.FirstOrDefault(p => p.Name == name) == null)
            {
                var project = new Project
                {
                    Name = name,
                    Description = description,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "Planning",
                    Priority = priority,
                    Manager = manager,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Projects.Add(project);
                Console.WriteLine($"✅ Projeto criado: {name}");
            }
        }

        context.SaveChanges();

        // Associar utilizadores a projetos
        AssociateUsersToProjects(context);
    }

    private static void AssociateUsersToProjects(ApplicationDbContext context)
    {
        var ana = context.Users.FirstOrDefault(u => u.Email == "ana@example.com");
        var carlos = context.Users.FirstOrDefault(u => u.Email == "carlos@example.com");
        var diana = context.Users.FirstOrDefault(u => u.Email == "diana@example.com");
        var eu = context.Users.FirstOrDefault(u => u.Email == "eu@example.com");

        if (ana == null || carlos == null || diana == null || eu == null) return;

        var projects = context.Projects.ToList();
        if (projects.Count < 5) return;

        // Website Redesign: Ana e Carlos
        AssociateUserToProject(context, projects[0].Id, ana.Id, "Membro");
        AssociateUserToProject(context, projects[0].Id, carlos.Id, "Lead");

        // Mobile App: Diana e Eu
        AssociateUserToProject(context, projects[1].Id, diana.Id, "Membro");
        AssociateUserToProject(context, projects[1].Id, eu.Id, "Lead");

        // API Integration: Ana
        AssociateUserToProject(context, projects[2].Id, ana.Id, "Lead");

        // Database Migration: Carlos e Diana
        AssociateUserToProject(context, projects[3].Id, carlos.Id, "Membro");
        AssociateUserToProject(context, projects[3].Id, diana.Id, "Lead");

        // Security Audit: Eu
        AssociateUserToProject(context, projects[4].Id, eu.Id, "Lead");

        context.SaveChanges();
    }

    private static void AssociateUserToProject(ApplicationDbContext context, int projectId, int userId, string role)
    {
        // Usar FirstOrDefault ao invés de Any para evitar CASE WHEN com TRUE/FALSE
        if (context.ProjectMembers.FirstOrDefault(pm => pm.ProjectId == projectId && pm.UserId == userId) == null)
        {
            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            context.ProjectMembers.Add(member);
            Console.WriteLine($"   → Utilizador associado ao projeto (Role: {role})");
        }
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
