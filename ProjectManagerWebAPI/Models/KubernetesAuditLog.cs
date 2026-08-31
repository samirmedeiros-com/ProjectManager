namespace ProjectManagerWebAPI.Models;

/// <summary>
/// Registo de uma ação na Gestão Kubernetes: quem, o quê, quando e com que resultado.
///
/// O nome e o email do utilizador ficam **copiados** para a linha, não só a chave estrangeira:
/// um utilizador que mude de nome ou seja desativado não pode reescrever nem apagar o passado.
/// </summary>
public class KubernetesAuditLog
{
    public int Id { get; set; }

    /// <summary>Id na tabela KubernetesUsers. Nulo num login falhado, em que não se sabe quem é.</summary>
    public int? UserId { get; set; }

    public required string UserEmail { get; set; }
    public string? UserName { get; set; }

    /// <summary>login, login-falhado, parar, arrancar, reiniciar.</summary>
    public required string Acao { get; set; }

    public string? Namespace { get; set; }
    public string? Deployment { get; set; }

    public bool Sucesso { get; set; } = true;

    /// <summary>Mensagem devolvida ao utilizador — a de sucesso ou a razão da falha.</summary>
    public string? Detalhe { get; set; }

    /// <summary>
    /// Estado antes e depois, quando a ação altera texto (a informação de um deployment).
    /// Guardam-se os dois para o registo responder sozinho a "o que é que mudou" — sem isto
    /// só se saberia que alguém mexeu.
    /// </summary>
    public string? ValorAnterior { get; set; }

    public string? ValorNovo { get; set; }

    public string? IpOrigem { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
