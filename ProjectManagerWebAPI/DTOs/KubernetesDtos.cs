namespace ProjectManagerWebAPI.DTOs;

public class K8sLoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class K8sUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class K8sLoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
    public K8sUserDto? User { get; set; }
}

public class K8sUserDetailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class CreateK8sUserDto
{
    public required string Email { get; set; }
    public required string FullName { get; set; }

    /// <summary>Admin, Operador ou Leitor. Só o Admin gere utilizadores; o Leitor não executa comandos.</summary>
    public string Role { get; set; } = "Leitor";
}

public class CreateK8sUserResponseDto
{
    public K8sUserDetailDto User { get; set; } = null!;
    public string TempPassword { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
}

public class FiltroAuditoria
{
    public string? Namespace { get; set; }
    public string? Deployment { get; set; }
    public string? Acao { get; set; }

    /// <summary>Procura no email e no nome de quem executou.</summary>
    public string? Utilizador { get; set; }

    public int Pagina { get; set; }
    public int Tamanho { get; set; } = 25;
}

public class RegistoAuditoriaDto
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public string? Deployment { get; set; }
    public bool Sucesso { get; set; }
    public string? Detalhe { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNovo { get; set; }
    public string? IpOrigem { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class PaginaAuditoria
{
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int Tamanho { get; set; }
    public List<RegistoAuditoriaDto> Registos { get; set; } = [];
}

public class NotaDeploymentDto
{
    public string? Titulo { get; set; }
    public string? Memo { get; set; }
    public string? AtualizadoPor { get; set; }
    public string? AtualizadoPorNome { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public class GravarNotaDto
{
    public string? Titulo { get; set; }
    public string? Memo { get; set; }
}
