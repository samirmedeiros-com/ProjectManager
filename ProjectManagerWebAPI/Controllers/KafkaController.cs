using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Filters;
using ProjectManagerWebAPI.Models.Kafka;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

/// <summary>
/// Kafka — módulo da Gestão de Dados sobre a REST API do Kafka Connect. Mostra os conectores
/// que replicam o AS400 para Oracle, o estado de cada um e das suas tasks, e deixa reiniciar
/// uma task falhada. Partilha o acesso com o resto da Gestão de Dados.
/// </summary>
[ApiController]
[Route("api/kafka")]
[Authorize]
[RequerApp(ContasController.AplicacaoNecessaria)]
public class KafkaController(
    IKafkaConnectGateway gateway,
    ILogger<KafkaController> logger) : ControllerBase
{
    /// <summary>Todos os conectores com estado e tasks, os partidos primeiro.</summary>
    [HttpGet("conectores")]
    public Task<ActionResult<ResumoKafka>> Conectores(CancellationToken ct)
        => ExecutarAsync(() => gateway.ListarAsync(ct), "listar os conectores");

    /// <summary>O estado de um conector, com o traço de erro das tasks falhadas.</summary>
    [HttpGet("conectores/{nome}")]
    public Task<ActionResult<Conector>> Conector(string nome, CancellationToken ct)
        => ExecutarAsync(async () => await gateway.ObterAsync(nome, ct)
                ?? throw new KafkaException($"O conector {nome} não existe."),
            $"ler o conector {nome}");

    /// <summary>
    /// Reinicia uma task. O Connect aceita com 204 e sem corpo: o estado novo só aparece na
    /// listagem seguinte, por isso o ecrã volta a ler passados alguns segundos.
    /// </summary>
    [HttpPost("conectores/{nome}/tasks/{task:int}/reiniciar")]
    public async Task<ActionResult<ResultadoReinicio>> ReiniciarTask(string nome, int task, CancellationToken ct)
    {
        var utilizador = User.FindFirst("email")?.Value ?? User.FindFirst("name")?.Value ?? "?";
        logger.LogInformation("Reinício da task {Task} de {Conector} pedido por {Utilizador}", task, nome, utilizador);

        try
        {
            return Ok(await gateway.ReiniciarTaskAsync(nome, task, ct));
        }
        catch (KafkaException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao reiniciar a task {Task} de {Conector}", task, nome);
            return StatusCode(StatusCodes.Status502BadGateway, $"Não foi possível reiniciar: {ex.Message}");
        }
    }

    private async Task<ActionResult<T>> ExecutarAsync<T>(Func<Task<T>> operacao, string descricao)
    {
        try
        {
            return Ok(await operacao());
        }
        catch (KafkaException ex)
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
