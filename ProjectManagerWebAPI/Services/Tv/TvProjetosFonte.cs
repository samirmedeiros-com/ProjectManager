using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services.Tv;

/// <summary>
/// Projetos e tarefas do Project Manager.
///
/// Estratégia deliberada: trazer projeções leves (sem entidades completas nem Include)
/// e agregar em memória — são poucas dezenas de linhas. Evita ainda duas armadilhas do
/// provider Oracle neste projeto: <c>Any()</c> traduz para literais booleanos que o
/// Oracle não tem (ORA-00904), e colunas <c>bool</c> não servem de predicado SQL mesmo
/// com <c>HasConversion&lt;int&gt;()</c>.
/// </summary>
public class TvProjetosFonte : ITvFonte
{
    public string Chave => "projetos";

    // Estados como estão gravados na coluna Status (ver statusOptions do dashboard).
    private static readonly Dictionary<string, string> RotulosEstado = new()
    {
        ["Planning"] = "Planeamento",
        ["Released"] = "Por Iniciar",
        ["Development"] = "Desenvolvimento",
        ["Completed"] = "Concluído",
        ["On Hold"] = "Em Espera",
        ["Finished"] = "Finalizado"
    };

    // Um projeto "fechado" não conta como ativo nem pode estar atrasado.
    private static readonly HashSet<string> EstadosFechados = ["Completed", "Finished"];

    private readonly ApplicationDbContext _db;
    private readonly TvDashboardOptions _opcoes;

    public TvProjetosFonte(ApplicationDbContext db, IOptions<TvDashboardOptions> opcoes)
    {
        _db = db;
        _opcoes = opcoes.Value;
    }

    public async Task<object> ObterAsync(CancellationToken ct)
    {
        var hoje = DateTime.Today;
        var limite = Math.Max(1, _opcoes.LimiteLinhas);

        var setores = await _db.Setores.AsNoTracking()
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(ct);

        var nomeSetor = setores.ToDictionary(s => s.Id, s => s.Name);

        var utilizadores = await _db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.Email, Nome = u.FullName })
            .ToListAsync(ct);

        var nomeUtilizador = utilizadores.ToDictionary(u => u.Id, u => u.Nome);

        // Manager e AssignedTo guardam o email, não o nome. Num ecrã de parede um
        // endereço lê-se mal e ocupa a linha toda — traduz-se sempre que der.
        var nomePorEmail = utilizadores
            .GroupBy(u => u.Email, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Nome, StringComparer.OrdinalIgnoreCase);

        var projetos = await _db.Projects.AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.Status, p.SetorId, p.OwnerId, p.Manager, p.EndDate, p.CompletedAt })
            .ToListAsync(ct);

        var tarefas = await _db.ProjectTasks.AsNoTracking()
            .Select(t => new { t.ProjectId, t.Title, t.Status, t.Priority, t.DueDate, t.AssignedTo, t.Progress })
            .ToListAsync(ct);

        var nomeProjeto = projetos.ToDictionary(p => p.Id, p => p.Name);

        var abertos = projetos.Where(p => !EstadosFechados.Contains(p.Status ?? string.Empty)).ToList();
        var atrasados = abertos.Where(p => p.EndDate.HasValue && p.EndDate.Value.Date < hoje).ToList();

        var tarefasAbertas = tarefas
            .Where(t => !string.Equals(t.Status, "Concluído", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(t.Status, "Cancelado", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // O progresso do projeto é a média do progresso das suas tarefas: é o único
        // sinal de avanço que existe em BD — não há campo de progresso no projeto.
        var progressoPorProjeto = tarefas
            .GroupBy(t => t.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => (int)Math.Round(g.Average(t =>
                    string.Equals(t.Status, "Concluído", StringComparison.OrdinalIgnoreCase)
                        ? 100m
                        : t.Progress ?? 0m)));

        TvTarefaLinhaDto Linha(string titulo, int projetoId, string? prioridade, DateTime prazo, string? atribuido) => new()
        {
            Titulo = titulo,
            Projeto = nomeProjeto.TryGetValue(projetoId, out var n) ? n : "—",
            Responsavel = Pessoa(atribuido, nomePorEmail),
            Prioridade = prioridade ?? string.Empty,
            Prazo = prazo,
            DiasParaPrazo = (int)(prazo.Date - hoje).TotalDays
        };

        return new TvProjetosDto
        {
            TotalProjetos = projetos.Count,
            ProjetosAtivos = abertos.Count,
            ProjetosConcluidos = projetos.Count(p => EstadosFechados.Contains(p.Status ?? string.Empty)),
            ProjetosAtrasados = atrasados.Count,
            TaxaNoPrazo = abertos.Count == 0
                ? 100
                : (int)Math.Round(100.0 * (abertos.Count - atrasados.Count) / abertos.Count),
            TarefasAbertas = tarefasAbertas.Count,
            TarefasAtrasadas = tarefasAbertas.Count(t => t.DueDate.Date < hoje),
            ConcluidosEsteMes = projetos.Count(p => p.CompletedAt.HasValue
                && p.CompletedAt.Value.Year == hoje.Year
                && p.CompletedAt.Value.Month == hoje.Month),

            PorEstado = RotulosEstado
                .Select(par => new TvFatiaDto { Rotulo = par.Value, Total = projetos.Count(p => p.Status == par.Key) })
                .ToList(),

            PorSetor = abertos
                .GroupBy(p => p.SetorId.HasValue && nomeSetor.TryGetValue(p.SetorId.Value, out var n) ? n : "Sem setor")
                .Select(g => new TvFatiaDto { Rotulo = g.Key, Total = g.Count() })
                .OrderByDescending(f => f.Total)
                .ToList(),

            CargaPorResponsavel = tarefasAbertas
                .GroupBy(t => Pessoa(t.AssignedTo, nomePorEmail))
                .Select(g => new TvFatiaDto { Rotulo = g.Key, Total = g.Count() })
                .OrderByDescending(f => f.Total)
                .Take(limite)
                .ToList(),

            EmCurso = abertos
                .OrderBy(p => p.EndDate ?? DateTime.MaxValue)
                .Take(limite)
                .Select(p => new TvProjetoLinhaDto
                {
                    Nome = p.Name,
                    Estado = Rotulo(p.Status),
                    Setor = p.SetorId.HasValue && nomeSetor.TryGetValue(p.SetorId.Value, out var n) ? n : "—",
                    Responsavel = ResponsavelDe(p.Manager, p.OwnerId, nomeUtilizador, nomePorEmail),
                    Progresso = progressoPorProjeto.TryGetValue(p.Id, out var prog) ? prog : 0,
                    Fim = p.EndDate,
                    DiasRestantes = p.EndDate.HasValue ? (int)(p.EndDate.Value.Date - hoje).TotalDays : null,
                    Atrasado = p.EndDate.HasValue && p.EndDate.Value.Date < hoje
                })
                .ToList(),

            TarefasEmAtraso = tarefasAbertas
                .Where(t => t.DueDate.Date < hoje)
                .OrderBy(t => t.DueDate)
                .Take(limite)
                .Select(t => Linha(t.Title, t.ProjectId, t.Priority, t.DueDate, t.AssignedTo))
                .ToList(),

            TarefasProximas = tarefasAbertas
                .Where(t => t.DueDate.Date >= hoje && t.DueDate.Date <= hoje.AddDays(7))
                .OrderBy(t => t.DueDate)
                .Take(limite)
                .Select(t => Linha(t.Title, t.ProjectId, t.Priority, t.DueDate, t.AssignedTo))
                .ToList()
        };
    }

    private static string Rotulo(string? estado) =>
        estado is not null && RotulosEstado.TryGetValue(estado, out var r) ? r : estado ?? "—";

    /// <summary>Nome legível de quem está atribuído; o campo guarda email.</summary>
    private static string Pessoa(string? valor, IReadOnlyDictionary<string, string> porEmail)
    {
        if (string.IsNullOrWhiteSpace(valor)) return "Por atribuir";
        return porEmail.TryGetValue(valor, out var nome) ? nome : valor;
    }

    /// <summary>O gestor gravado no projeto prevalece; caindo esse, mostra-se o dono.</summary>
    private static string ResponsavelDe(
        string? manager, int? ownerId,
        IReadOnlyDictionary<int, string> porId,
        IReadOnlyDictionary<string, string> porEmail)
    {
        if (!string.IsNullOrWhiteSpace(manager))
            return porEmail.TryGetValue(manager, out var nomeGestor) ? nomeGestor : manager;

        if (ownerId.HasValue && porId.TryGetValue(ownerId.Value, out var nomeDono)) return nomeDono;

        return "—";
    }
}
