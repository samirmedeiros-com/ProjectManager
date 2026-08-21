using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services.Tv;

public interface ITvDashboardService
{
    Task<TvRespostaDto> ObterAsync(CancellationToken ct = default);
}

/// <summary>
/// Junta os blocos das várias fontes numa só resposta.
///
/// As fontes correm em paralelo, cada uma no seu próprio scope de injeção: o
/// DbContext não é thread-safe e partilhá-lo entre tarefas concorrentes rebenta
/// com "A second operation was started on this context instance". Um scope por
/// fonte dá a cada uma o seu contexto.
///
/// Cada fonte é também isolada nas falhas: se a operação SEUR falhar — tabela
/// bloqueada, query demorada — os cards de projetos continuam a aparecer, e a
/// fonte em falha vai em FontesEmFalha para o ecrã poder assinalá-lo. Num mural
/// que ninguém vigia, meio ecrã certo vale mais do que um ecrã em branco.
/// </summary>
public class TvDashboardService : ITvDashboardService
{
    private const string ChaveCache = "tv:dashboard";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly TvDashboardOptions _opcoes;

    public TvDashboardService(IServiceScopeFactory scopeFactory, IMemoryCache cache, IOptions<TvDashboardOptions> opcoes)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _opcoes = opcoes.Value;
    }

    /// <summary>
    /// Os dados servidos são partilhados por todos os ecrãs durante metade do
    /// intervalo de refresh. Sem isto, cada TV ligada multiplica a carga sobre
    /// tabelas de produção — e todas veriam exatamente os mesmos números.
    /// </summary>
    public Task<TvRespostaDto> ObterAsync(CancellationToken ct = default)
    {
        var janela = TimeSpan.FromSeconds(Math.Max(5, _opcoes.RefreshSegundos / 2.0));

        return _cache.GetOrCreateAsync(ChaveCache, entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = janela;
            return CalcularAsync(ct);
        })!;
    }

    private async Task<TvRespostaDto> CalcularAsync(CancellationToken ct)
    {
        var resposta = new TvRespostaDto
        {
            GeradoEm = DateTime.Now,
            RefreshSegundos = _opcoes.RefreshSegundos
        };

        // Descobrir que fontes estão registadas, sem ainda as executar.
        List<Type> tipos;
        using (var scope = _scopeFactory.CreateScope())
        {
            tipos = scope.ServiceProvider.GetServices<ITvFonte>().Select(f => f.GetType()).ToList();
        }

        var resultados = await Task.WhenAll(tipos.Select(tipo => ExecutarAsync(tipo, ct)));

        foreach (var r in resultados)
        {
            if (r.Falhou) resposta.FontesEmFalha.Add(r.Chave);
            else resposta.Fontes[r.Chave] = r.Dados!;
        }

        return resposta;
    }

    private async Task<(string Chave, object? Dados, bool Falhou)> ExecutarAsync(Type tipo, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var fonte = scope.ServiceProvider.GetServices<ITvFonte>().First(f => f.GetType() == tipo);
        var crono = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var dados = await fonte.ObterAsync(ct);

            // O tempo de cada fonte fica no log: quando o mural abranda, é a única
            // forma de saber qual delas é, sem ter de adivinhar.
            Console.WriteLine($"[TV] Fonte '{fonte.Chave}': {crono.ElapsedMilliseconds}ms");
            return (fonte.Chave, dados, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TV] Fonte '{fonte.Chave}' falhou ao fim de {crono.ElapsedMilliseconds}ms: {ex}");
            return (fonte.Chave, null, true);
        }
    }
}
