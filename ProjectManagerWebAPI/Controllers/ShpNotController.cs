using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Filters;
using ProjectManagerWebAPI.Models.Contas;
using ProjectManagerWebAPI.Models.ShpNot;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

/// <summary>
/// ShpNot — módulo da Gestão de Dados sobre os SHPNOTs recebidos do Geopost e guardados pelo
/// WebApiShpNot. É um ecrã de consulta: nenhum destes caminhos escreve.
/// </summary>
[ApiController]
[Route("api/shpnot")]
[Authorize]
[RequerApp(ContasController.AplicacaoNecessaria)]
public class ShpNotController(
    IShpNotRepository repositorio,
    ILogger<ShpNotController> logger) : ControllerBase
{
    /// <summary>O estado da fila para o AS400 e o último SHPNOT recebido.</summary>
    [HttpGet("estatisticas")]
    public Task<ActionResult<ShpNotEstatisticas>> Estatisticas(CancellationToken ct)
        => ExecutarAsync(() => repositorio.EstatisticasAsync(ct), "obter o estado da fila");

    /// <summary>Pesquisa paginada dos SHPNOTs recebidos.</summary>
    [HttpGet]
    public Task<ActionResult<FatiaShpNot>> Procurar(
        [FromQuery] DateTime? data,
        [FromQuery] string? mpsid,
        [FromQuery] string? volume,
        [FromQuery] string? estado,
        [FromQuery] string? respserv,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10,
        CancellationToken ct = default)
        => ExecutarAsync(
            () => repositorio.ProcurarAsync(
                new FiltroShpNot(data, mpsid, volume, estado, respserv, pagina, tamanho), ct),
            "procurar SHPNOTs");

    /// <summary>
    /// Um SHPNOT inteiro, já repartido pelas abas do ecrã e com cada campo acompanhado do
    /// nome que tem no JSON, da tabela e coluna onde está, e do alias da VW_SHPNOT_AS400.
    /// </summary>
    [HttpGet("{idt:long}")]
    public async Task<ActionResult<ShpNotDetalhe>> Obter(long idt, CancellationToken ct)
    {
        var resposta = await ExecutarAsync(() => repositorio.ObterAsync(idt, ct), "abrir o SHPNOT");
        if (resposta.Result is not null) return resposta.Result;
        return resposta.Value is null ? NotFound("SHPNOT não encontrado.") : resposta.Value;
    }

    private async Task<ActionResult<T>> ExecutarAsync<T>(Func<Task<T>> operacao, string oQue)
    {
        try
        {
            return await operacao();
        }
        catch (ShpNotException erro)
        {
            logger.LogWarning(erro, "Falha ao {OQue}.", oQue);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, erro.Message);
        }
        catch (Exception erro)
        {
            logger.LogError(erro, "Erro inesperado ao {OQue}.", oQue);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Não foi possível {oQue}.");
        }
    }
}
