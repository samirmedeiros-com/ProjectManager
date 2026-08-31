using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services;

public interface IKubernetesNotaService
{
    Task<NotaDeploymentDto?> ObterAsync(string ns, string deployment);

    /// <summary>Títulos de todos os deployments de um namespace, para a lista os mostrar.</summary>
    Task<Dictionary<string, string>> TitulosAsync(string ns);

    /// <summary>Grava e devolve o estado anterior, para o registo poder dizer o que mudou.</summary>
    Task<(NotaDeploymentDto Nova, NotaDeploymentDto? Anterior)> GravarAsync(
        string ns, string deployment, GravarNotaDto dados, string email, string? nome);
}

public class KubernetesNotaService : IKubernetesNotaService
{
    public const int TituloMaximo = 100;

    private readonly ApplicationDbContext _context;

    public KubernetesNotaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotaDeploymentDto?> ObterAsync(string ns, string deployment)
    {
        var nota = await Procurar(ns, deployment);
        return nota is null ? null : Converter(nota);
    }

    public async Task<Dictionary<string, string>> TitulosAsync(string ns)
    {
        var notas = await _context.KubernetesDeploymentNotas
            .Where(n => n.Namespace == ns)
            .Select(n => new { n.Deployment, n.Titulo })
            .ToListAsync();

        return notas
            .Where(n => !string.IsNullOrWhiteSpace(n.Titulo))
            .ToDictionary(n => n.Deployment, n => n.Titulo!, StringComparer.Ordinal);
    }

    public async Task<(NotaDeploymentDto Nova, NotaDeploymentDto? Anterior)> GravarAsync(
        string ns, string deployment, GravarNotaDto dados, string email, string? nome)
    {
        var titulo = Limpar(dados.Titulo);
        var memo = Limpar(dados.Memo);

        if (titulo is { Length: > TituloMaximo })
            titulo = titulo[..TituloMaximo];

        var nota = await Procurar(ns, deployment);
        var anterior = nota is null ? null : Converter(nota);

        if (nota is null)
        {
            nota = new KubernetesDeploymentNota { Namespace = ns, Deployment = deployment };
            _context.KubernetesDeploymentNotas.Add(nota);
        }

        nota.Titulo = titulo;
        nota.Memo = memo;
        nota.AtualizadoPor = email;
        nota.AtualizadoPorNome = nome;
        nota.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (Converter(nota), anterior);
    }

    private Task<KubernetesDeploymentNota?> Procurar(string ns, string deployment)
        => _context.KubernetesDeploymentNotas
            .FirstOrDefaultAsync(n => n.Namespace == ns && n.Deployment == deployment);

    /// <summary>Vazio e só-espaços são a mesma coisa que "não há nota": guarda-se null.</summary>
    private static string? Limpar(string? texto)
        => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static NotaDeploymentDto Converter(KubernetesDeploymentNota n) => new()
    {
        Titulo = n.Titulo,
        Memo = n.Memo,
        AtualizadoPor = n.AtualizadoPor,
        AtualizadoPorNome = n.AtualizadoPorNome,
        AtualizadoEm = n.AtualizadoEm
    };
}
