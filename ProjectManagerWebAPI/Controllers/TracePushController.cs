using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Filters;
using ProjectManagerWebAPI.Models.Contas;
using ProjectManagerWebAPI.Models.TracePush;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

/// <summary>
/// Trace Push — módulo da Gestão de Dados sobre CHRONO_WEB.CW_TRACEPUSH, a fila de envio de
/// eventos de trace para os webservices dos clientes. Partilha o acesso com o resto da Gestão
/// de Dados: credenciais da Gestão SEUR e o claim "app" verificado pelo [RequerApp].
/// </summary>
[ApiController]
[Route("api/tracepush")]
[Authorize]
[RequerApp(ContasController.AplicacaoNecessaria)]
public class TracePushController(
    ITracePushRepository repositorio,
    ITracePushAuditService auditoria,
    ILogger<TracePushController> logger) : ControllerBase
{
    /// <summary>Os cartões de valores acumulados. Sem data é o dia de hoje.</summary>
    [HttpGet("estatisticas")]
    public Task<ActionResult<PushEstatisticas>> Estatisticas([FromQuery] DateTime? data, CancellationToken ct)
        => ExecutarAsync(() => repositorio.EstatisticasAsync(data ?? DateTime.Today, ct), "obter os valores do dia");

    /// <summary>Report por userlogin: enviados, erros e pendentes de um dia.</summary>
    [HttpGet("por-userlogin")]
    public Task<ActionResult<List<PushPorUserLogin>>> PorUserLogin([FromQuery] DateTime? data, CancellationToken ct)
        => ExecutarAsync(() => repositorio.PorUserLoginAsync(data ?? DateTime.Today, ct), "obter o report por userlogin");

    /// <summary>As 24 barras do gráfico de um userlogin no dia escolhido.</summary>
    [HttpGet("por-hora")]
    public Task<ActionResult<List<PushPorHora>>> PorHora(
        [FromQuery] string userlogin, [FromQuery] DateTime? data, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userlogin))
            return Task.FromResult<ActionResult<List<PushPorHora>>>(BadRequest("Escolha um userlogin para ver o gráfico."));

        return ExecutarAsync(() => repositorio.PorHoraAsync(data ?? DateTime.Today, userlogin.Trim(), ct),
            $"obter o gráfico de {userlogin}");
    }

    /// <summary>
    /// Pesquisa paginada. A data é obrigatória <b>excepto</b> quando se indica um número de
    /// guia — ver <see cref="TracePushRepository"/> para o porquê.
    /// </summary>
    [HttpGet]
    public Task<ActionResult<Pagina<PushResumo>>> Procurar(
        [FromQuery] DateTime? data,
        [FromQuery] string? userlogin,
        [FromQuery] string? conta,
        [FromQuery] string? guia,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10,
        CancellationToken ct = default)
        => ExecutarAsync(
            () => repositorio.ProcurarAsync(new FiltroPush(data, userlogin, conta, guia, estado, pagina, tamanho), ct),
            "procurar os envios");

    /// <summary>O detalhe do popup, com pedido e resposta.</summary>
    [HttpGet("{hhpRowId}")]
    public Task<ActionResult<PushDetalhe>> Obter(string hhpRowId, CancellationToken ct)
        => ExecutarAsync(async () => await repositorio.ObterAsync(hhpRowId, ct)
                ?? throw new TracePushException($"O envio {hhpRowId} não existe."),
            $"ler o envio {hhpRowId}");

    /// <summary>
    /// Repõe as linhas na fila (FLAG='N'). Não chama o webservice do cliente — quem envia é
    /// o processo automático de push.
    /// </summary>
    [HttpPost("reenviar")]
    public async Task<ActionResult<ResultadoReenvio>> Reenviar([FromBody] PedidoReenvio pedido, CancellationToken ct)
    {
        var ids = (pedido?.HhpRowIds ?? [])
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim())
            .Distinct()
            .ToList();

        if (ids.Count == 0) return BadRequest("Escolha pelo menos um envio para reenviar.");

        var utilizador = User.FindFirst("email")?.Value ?? User.FindFirst("name")?.Value ?? "?";

        try
        {
            // Lê-se antes do UPDATE: a flag anterior é o que se perde ao repor, e é o que
            // dá sentido ao registo depois.
            var linhas = await repositorio.ObterVariosAsync(ids, ct);
            var encontrados = linhas.Select(l => l.HhpRowId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var repostos = await repositorio.ReporPendenteAsync(ids, ct);

            logger.LogInformation("Reenvio de {Repostos} envios pedido por {Utilizador}", repostos, utilizador);
            await auditoria.RegistarReenvioAsync(utilizador, linhas, sucesso: true, mensagem: null);

            return Ok(new ResultadoReenvio
            {
                Pedidos = ids.Count,
                Repostos = repostos,
                NaoEncontrados = [.. ids.Where(i => !encontrados.Contains(i))],
            });
        }
        catch (TracePushException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao reenviar {Total} envios", ids.Count);
            return StatusCode(StatusCodes.Status502BadGateway, $"Não foi possível reenviar: {ex.Message}");
        }
    }

    /// <summary>O registo de reenvios, do mais recente para o mais antigo.</summary>
    [HttpGet("logs")]
    public Task<ActionResult<List<ReenvioLog>>> Logs(
        [FromQuery] int limite = 100, [FromQuery] string? userlogin = null, [FromQuery] string? guia = null)
        => ExecutarAsync(() => auditoria.ListarAsync(limite, userlogin, guia), "listar o registo de reenvios");

    private async Task<ActionResult<T>> ExecutarAsync<T>(Func<Task<T>> operacao, string descricao)
    {
        try
        {
            return Ok(await operacao());
        }
        catch (TracePushException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao {Descricao}", descricao);
            return StatusCode(StatusCodes.Status502BadGateway, $"Não foi possível {descricao}: {ex.Message}");
        }
    }
}
