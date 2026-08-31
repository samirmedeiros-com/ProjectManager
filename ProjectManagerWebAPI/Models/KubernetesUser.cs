namespace ProjectManagerWebAPI.Models;

/// <summary>
/// Utilizador da Gestão Kubernetes. Tabela própria, sem ligação a Users nem a SeurUsers:
/// quem administra o cluster não é quem usa o Project Manager, e as credenciais não são
/// partilhadas com nenhuma das outras aplicações do portal.
/// </summary>
public class KubernetesUser
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required string PasswordHash { get; set; }

    /// <summary>Admin gere utilizadores e executa comandos; Operador executa comandos; Leitor só vê.</summary>
    public string? Role { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
