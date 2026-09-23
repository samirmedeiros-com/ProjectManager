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
    IShpNotSaidaRepository saida,
    ILogger<ShpNotController> logger) : ControllerBase
{
    /// <summary>Pesquisa paginada dos SHPNOTs recebidos.</summary>
    [HttpGet]
    public Task<ActionResult<FatiaShpNot>> Procurar(
        [FromQuery] DateTime? data,
        [FromQuery] string? mpsid,
        [FromQuery] string? volume,
        [FromQuery] string? estado,
        [FromQuery] string? respserv,
        [FromQuery] string? sentido,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10,
        CancellationToken ct = default)
        => ExecutarAsync(async () =>
        {
            var filtro = new FiltroShpNot(data, mpsid, volume, estado, respserv, pagina, tamanho, sentido);

            // O sentido escolhe as tabelas onde se procura, e não apenas o que se mostra:
            // IN são as ~65 tabelas do WebApiShpNot, OUT são a GEODT01SPN e a GEODT02SPN.
            // São bases de dados diferentes para o mesmo envio, cada uma com as suas chaves.
            var soSaida = sentido == SentidoShpNot.Saida;
            var soEntrada = sentido == SentidoShpNot.Entrada;

            if (soSaida)
            {
                var apenasEnviados = await saida.ProcurarAsync(filtro, ct);
                return Fatia(apenasEnviados, pagina, tamanho);
            }

            var recebidos = await repositorio.ProcurarAsync(filtro, ct);
            if (soEntrada) return recebidos;

            // Sem sentido escolhido vêm os dois lados — mas o da saída só quando há um número
            // para procurar. Sem ele seria uma varredura sem dono sobre milhões de linhas.
            if (string.IsNullOrWhiteSpace(mpsid) && string.IsNullOrWhiteSpace(volume))
                return recebidos;

            var enviados = await saida.ProcurarAsync(filtro, ct);
            if (enviados.Count == 0) return recebidos;

            // Os dois lados na mesma grelha, do mais recente para o mais antigo. É o mesmo
            // envio visto das duas pontas: o que recebemos e o que mandámos.
            return recebidos with
            {
                Itens = [.. recebidos.Itens.Concat(enviados).OrderByDescending(i => i.Recebido)],
            };
        }, "procurar SHPNOTs");

    /// <summary>Um SHPNOT que enviámos, com os campos arrumados nas mesmas abas.</summary>
    [HttpGet("saida/{idt:long}")]
    public async Task<ActionResult<ShpNotDetalhe>> ObterSaida(long idt, CancellationToken ct)
    {
        var resposta = await ExecutarAsync(() => saida.ObterAsync(idt, ct), "abrir o SHPNOT enviado");
        if (resposta.Result is not null) return resposta.Result;
        return resposta.Value is null ? NotFound("SHPNOT enviado não encontrado.") : resposta.Value;
    }

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

    /// <summary>
    /// Corta a lista dos enviados na página pedida. O repositório da saída devolve tudo até
    /// ao fim da página mais uma linha — é assim que se sabe se há página seguinte sem contar
    /// o resto.
    /// </summary>
    private static FatiaShpNot Fatia(List<ShpNotResumo> itens, int pagina, int tamanho)
    {
        var saltar = (Math.Max(pagina, 1) - 1) * tamanho;
        var pagina_ = itens.Skip(saltar).Take(tamanho + 1).ToList();
        var haMais = pagina_.Count > tamanho;
        if (haMais) pagina_.RemoveAt(pagina_.Count - 1);

        return new FatiaShpNot
        {
            Itens = pagina_,
            PaginaAtual = pagina,
            Tamanho = tamanho,
            HaMais = haMais,
        };
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
