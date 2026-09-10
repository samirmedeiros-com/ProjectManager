using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Filters;
using ProjectManagerWebAPI.Models.Contas;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

/// <summary>
/// Gestão de Dados — contas e subcontas (WSDPD.AS400_CONTAS / AS400_SUBCONTAS) e envio ao
/// portal de clientes. Não tem login próprio: usa as credenciais da Gestão SEUR, como a
/// Consulta OpenSearch. Todas as apps do portal assinam o JWT com a mesma chave, issuer e
/// audience, por isso [Authorize] sozinho aceitaria o token de qualquer uma — quem separa
/// é o claim "app" verificado pelo [RequerApp].
/// </summary>
[ApiController]
[Route("api/contas")]
[Authorize]
[RequerApp(AplicacaoNecessaria)]
public class ContasController(
    IContasRepository repositorio,
    IContasPortalSender emissor,
    IContasAuditService auditoria,
    ILogger<ContasController> logger) : ControllerBase
{
    public const string AplicacaoNecessaria = "seur";

    /// <summary>Confirma que o token serve esta aplicação. Serve o guard do Angular.</summary>
    [HttpGet("acesso")]
    public ActionResult<object> Acesso() => Ok(new { permitido = true, aplicacao = AplicacaoNecessaria });

    [HttpGet("estatisticas")]
    public Task<ActionResult<ContasEstatisticas>> Estatisticas(CancellationToken ct)
        => ExecutarAsync(() => repositorio.EstatisticasAsync(ct), "obter as estatísticas");

    [HttpGet]
    public Task<ActionResult<Pagina<ContaResumo>>> Listar(
        [FromQuery] string? procura, [FromQuery] string? flagportal,
        [FromQuery] int pagina = 1, [FromQuery] int tamanho = 10, CancellationToken ct = default)
        => ExecutarAsync(() => repositorio.ListarContasAsync(procura, flagportal, pagina, tamanho, ct), "listar as contas");

    [HttpGet("{eeent}")]
    public Task<ActionResult<Conta>> Obter(string eeent, CancellationToken ct)
        => ExecutarAsync(async () => await repositorio.ObterContaAsync(eeent, ct)
                ?? throw new ContasException($"A conta {eeent} não existe."),
            $"ler a conta {eeent}");

    [HttpPost]
    public Task<ActionResult<Conta>> Gravar([FromBody] Conta conta, CancellationToken ct)
        => ExecutarAsync(() => repositorio.GravarContaAsync(conta, ct), "gravar a conta");

    [HttpGet("{eeent}/subcontas")]
    public Task<ActionResult<List<SubConta>>> ListarSubContas(string eeent, CancellationToken ct)
        => ExecutarAsync(() => repositorio.ListarSubContasAsync(eeent, ct), $"listar as subcontas de {eeent}");

    [HttpGet("{eeent}/subcontas/{emend}")]
    public Task<ActionResult<SubConta>> ObterSubConta(string eeent, string emend, CancellationToken ct)
        => ExecutarAsync(async () => await repositorio.ObterSubContaAsync(eeent, emend, ct)
                ?? throw new ContasException($"A subconta {eeent}/{emend} não existe."),
            $"ler a subconta {eeent}/{emend}");

    [HttpPost("subcontas")]
    public Task<ActionResult<SubConta>> GravarSubConta([FromBody] SubConta subConta, CancellationToken ct)
        => ExecutarAsync(() => repositorio.GravarSubContaAsync(subConta, ct), "gravar a subconta");

    /// <summary>
    /// Envia ao portal. O ambiente vem sempre no pedido — não há default: mandar para produção
    /// por engano não é coisa que se desfaça.
    /// </summary>
    [HttpPost("enviar")]
    public Task<ActionResult<ResultadoEnvio>> Enviar([FromBody] PedidoEnvio pedido, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pedido.Conta))
            return Task.FromResult<ActionResult<ResultadoEnvio>>(BadRequest("É preciso indicar a conta."));

        var utilizador = User.FindFirst("email")?.Value ?? User.FindFirst("name")?.Value ?? "?";
        logger.LogInformation("Envio pedido por {Utilizador}: conta {Conta} subconta {Sub} para {Ambiente}",
            utilizador, pedido.Conta, pedido.SubConta ?? "(todas)", pedido.Ambiente);

        return ExecutarAsync(() => emissor.EnviarAsync(pedido, utilizador, ct), "enviar ao portal");
    }

    /// <summary>Últimos envios registados (cabeçalhos), do mais recente para o mais antigo.</summary>
    [HttpGet("logs")]
    public Task<ActionResult<List<EnvioLog>>> Logs([FromQuery] int limite = 100, [FromQuery] string? conta = null)
        => ExecutarAsync(() => auditoria.ListarAsync(limite, conta), "listar o registo de envios");

    /// <summary>As respostas de conta/subconta de um envio registado.</summary>
    [HttpGet("logs/{logId:long}/linhas")]
    public Task<ActionResult<List<EnvioLogLinha>>> LogLinhas(long logId)
        => ExecutarAsync(() => auditoria.LinhasAsync(logId), $"ler as respostas do envio {logId}");

    private async Task<ActionResult<T>> ExecutarAsync<T>(Func<Task<T>> operacao, string descricao)
    {
        try
        {
            return Ok(await operacao());
        }
        catch (ContasException ex)
        {
            // Erro de dados/utilização: a mensagem foi escrita para ser lida no ecrã.
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao {Descricao}", descricao);
            return StatusCode(StatusCodes.Status502BadGateway, $"Não foi possível {descricao}: {ex.Message}");
        }
    }
}
