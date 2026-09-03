using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Filters;
using ProjectManagerWebAPI.Models.OpenSearch;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

/// <summary>
/// Portal de consulta ao OpenSearch. Não tem login próprio: usa as credenciais da
/// Gestão SEUR (tabela SeurUsers). Como todas as apps do portal assinam o JWT com a
/// mesma chave, issuer e audience, [Authorize] sozinho aceitaria o token de qualquer
/// uma delas — quem separa é o claim "app" verificado pelo [RequerApp].
/// </summary>
[ApiController]
[Route("api/opensearch")]
[Authorize]
[RequerApp(AplicacaoNecessaria)]
public class OpenSearchController : ControllerBase
{
    public const string AplicacaoNecessaria = "seur";

    private readonly OpenSearchGateway _gateway;
    private readonly ILogger<OpenSearchController> _logger;

    public OpenSearchController(OpenSearchGateway gateway, ILogger<OpenSearchController> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    /// <summary>
    /// Confirma que o token do utilizador atual serve este portal. Serve o guard do
    /// Angular, que assim valida a sessão contra o servidor antes de abrir o ecrã.
    /// </summary>
    [HttpGet("acesso")]
    public ActionResult<object> Acesso() => Ok(new { permitido = true, aplicacao = AplicacaoNecessaria });

    [HttpGet("estado")]
    public Task<ActionResult<EstadoCluster>> Estado(CancellationToken ct)
        => ExecutarAsync(() => _gateway.ObterEstadoAsync(ct), "obter o estado do cluster");

    [HttpGet("indices")]
    public Task<ActionResult<IReadOnlyList<IndiceInfo>>> Indices(CancellationToken ct)
        => ExecutarAsync(() => _gateway.ListarIndicesAsync(ct), "listar os índices");

    [HttpGet("indices/{indice}/campos")]
    public Task<ActionResult<IReadOnlyList<CampoInfo>>> Campos(string indice, CancellationToken ct)
        => ExecutarAsync(() => _gateway.ListarCamposAsync(indice, ct), $"ler os campos de '{indice}'");

    [HttpPost("pesquisa")]
    public Task<ActionResult<ResultadoPesquisa>> Pesquisa([FromBody] PedidoPesquisa pedido, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pedido.Indice))
            return Task.FromResult<ActionResult<ResultadoPesquisa>>(BadRequest("É preciso escolher um índice."));

        return ExecutarAsync(() => _gateway.PesquisarAsync(pedido, ct), $"pesquisar em '{pedido.Indice}'");
    }

    /// <summary>
    /// Converte as falhas do cluster em respostas legíveis: 502 com a razão devolvida pelo
    /// OpenSearch, nunca uma excepção crua.
    /// </summary>
    private async Task<ActionResult<T>> ExecutarAsync<T>(Func<Task<T>> operacao, string descricao)
    {
        try
        {
            return Ok(await operacao());
        }
        catch (OpenSearchException ex)
        {
            _logger.LogError(ex, "Falha ao {Descricao}.", descricao);
            return StatusCode(502, ex.Message);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Pedido cancelado pelo cliente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao {Descricao}.", descricao);
            return StatusCode(500, $"Erro inesperado ao {descricao}.");
        }
    }
}
