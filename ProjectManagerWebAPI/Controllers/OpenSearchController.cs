using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Filters;
using ProjectManagerWebAPI.Models.OpenSearch;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

/// <summary>
/// Portal de consulta ao OpenSearch. Autentica com o token do Project Manager
/// (não tem login próprio, ao contrário do SEUR e do OraConsole) e restringe o
/// acesso aos utilizadores do setor IT.
/// </summary>
[ApiController]
[Route("api/opensearch")]
[Authorize]
[RequerSetor(SetorNecessario)]
public class OpenSearchController : ControllerBase
{
    public const string SetorNecessario = "IT";

    private readonly OpenSearchGateway _gateway;
    private readonly ILogger<OpenSearchController> _logger;

    public OpenSearchController(OpenSearchGateway gateway, ILogger<OpenSearchController> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    /// <summary>
    /// Confirma que o utilizador atual pode usar o portal. Serve o guard do Angular,
    /// que não consegue ver o setor pelo token — o setor não vai nos claims.
    /// </summary>
    [HttpGet("acesso")]
    public ActionResult<object> Acesso() => Ok(new { permitido = true, setor = SetorNecessario });

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
