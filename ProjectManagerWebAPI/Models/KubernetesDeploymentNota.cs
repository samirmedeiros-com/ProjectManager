namespace ProjectManagerWebAPI.Models;

/// <summary>
/// Informação que a equipa escreve sobre um deployment — para que serve, cuidados a ter,
/// quem é o dono. Vive na base de dados e não em anotações do cluster: é conhecimento da
/// equipa, não configuração, e um redeploy não o pode levar à frente.
/// </summary>
public class KubernetesDeploymentNota
{
    public int Id { get; set; }

    public required string Namespace { get; set; }
    public required string Deployment { get; set; }

    /// <summary>Aparece por baixo do nome na lista, por isso é curto: 100 caracteres.</summary>
    public string? Titulo { get; set; }

    public string? Memo { get; set; }

    public string? AtualizadoPor { get; set; }
    public string? AtualizadoPorNome { get; set; }
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
