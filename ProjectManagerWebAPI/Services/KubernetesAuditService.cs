using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services;

public interface IKubernetesAuditService
{
    Task RegistarAsync(
        int? userId, string email, string? nome, string acao,
        string? ns = null, string? deployment = null,
        bool sucesso = true, string? detalhe = null, string? ip = null,
        string? valorAnterior = null, string? valorNovo = null);

    Task<PaginaAuditoria> ConsultarAsync(FiltroAuditoria filtro);
}

/// <summary>
/// Escreve e lê o registo de ações. As ações desta aplicação mexem em produção, por isso
/// nenhuma delas passa sem ficar gravada.
/// </summary>
public class KubernetesAuditService : IKubernetesAuditService
{
    public const string AcaoLogin = "login";
    public const string AcaoLoginFalhado = "login-falhado";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<KubernetesAuditService> _logger;

    public KubernetesAuditService(ApplicationDbContext context, ILogger<KubernetesAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public const string AcaoNota = "nota";

    public async Task RegistarAsync(
        int? userId, string email, string? nome, string acao,
        string? ns = null, string? deployment = null,
        bool sucesso = true, string? detalhe = null, string? ip = null,
        string? valorAnterior = null, string? valorNovo = null)
    {
        try
        {
            _context.KubernetesAuditLogs.Add(new KubernetesAuditLog
            {
                UserId = userId,
                UserEmail = email,
                UserName = nome,
                Acao = acao,
                Namespace = ns,
                Deployment = deployment,
                Sucesso = sucesso,
                // A coluna é NVARCHAR2: um detalhe muito longo faria a gravação rebentar e,
                // com ela, o pedido que se estava a registar.
                Detalhe = detalhe is { Length: > 1000 } ? detalhe[..1000] : detalhe,
                // Sem truncar: são CLOB, e a razão de existirem é poder comparar o texto todo.
                ValorAnterior = valorAnterior,
                ValorNovo = valorNovo,
                IpOrigem = ip,
                CriadoEm = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Falhar a gravar o registo não pode desfazer a ação que já aconteceu no cluster,
            // nem devolver um erro a quem a executou com sucesso. Fica no log da aplicação.
            _logger.LogError(ex, "Não foi possível gravar o registo da ação {Acao} de {Email}.", acao, email);
        }
    }

    public async Task<PaginaAuditoria> ConsultarAsync(FiltroAuditoria filtro)
    {
        var pagina = filtro.Pagina < 0 ? 0 : filtro.Pagina;
        var tamanho = Math.Clamp(filtro.Tamanho <= 0 ? 25 : filtro.Tamanho, 1, 200);

        var consulta = _context.KubernetesAuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Namespace))
            consulta = consulta.Where(l => l.Namespace == filtro.Namespace);

        if (!string.IsNullOrWhiteSpace(filtro.Deployment))
            consulta = consulta.Where(l => l.Deployment == filtro.Deployment);

        if (!string.IsNullOrWhiteSpace(filtro.Acao))
            consulta = consulta.Where(l => l.Acao == filtro.Acao);

        if (!string.IsNullOrWhiteSpace(filtro.Utilizador))
        {
            var termo = filtro.Utilizador.Trim().ToLower();
            consulta = consulta.Where(l =>
                l.UserEmail.ToLower().Contains(termo) ||
                (l.UserName != null && l.UserName.ToLower().Contains(termo)));
        }

        var total = await consulta.CountAsync();

        // Descendente: o que interessa é sempre o que aconteceu agora. O Id desempata as
        // linhas gravadas no mesmo instante, senão a paginação podia repetir ou saltar uma.
        var linhas = await consulta
            .OrderByDescending(l => l.CriadoEm)
            .ThenByDescending(l => l.Id)
            .Skip(pagina * tamanho)
            .Take(tamanho)
            .Select(l => new RegistoAuditoriaDto
            {
                Id = l.Id,
                UserEmail = l.UserEmail,
                UserName = l.UserName,
                Acao = l.Acao,
                Namespace = l.Namespace,
                Deployment = l.Deployment,
                Sucesso = l.Sucesso,
                Detalhe = l.Detalhe,
                ValorAnterior = l.ValorAnterior,
                ValorNovo = l.ValorNovo,
                IpOrigem = l.IpOrigem,
                CriadoEm = l.CriadoEm
            })
            .ToListAsync();

        return new PaginaAuditoria
        {
            Total = total,
            Pagina = pagina,
            Tamanho = tamanho,
            Registos = linhas
        };
    }
}
