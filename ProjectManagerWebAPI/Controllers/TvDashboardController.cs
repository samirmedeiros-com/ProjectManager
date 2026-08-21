using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Filters;
using ProjectManagerWebAPI.Services.Tv;

namespace ProjectManagerWebAPI.Controllers;

/// <summary>
/// Mural de TV — somente leitura e sem sessão. Não usa JWT: o acesso é a chave
/// partilhada validada por <see cref="RequerChaveTvAttribute"/>.
/// </summary>
[Route("api/tv")]
[ApiController]
[AllowAnonymous]
[RequerChaveTv]
public class TvDashboardController : ControllerBase
{
    private readonly ITvDashboardService _servico;

    public TvDashboardController(ITvDashboardService servico)
    {
        _servico = servico;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> ObterDashboard(CancellationToken ct)
    {
        try
        {
            return Ok(await _servico.ObterAsync(ct));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TV] Erro ao construir o mural: {ex}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensagem = "Não foi possível obter os dados do mural." });
        }
    }

    /// <summary>Sonda leve para o ecrã distinguir chave errada de servidor em baixo.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { estado = "ok", agora = DateTime.Now });
}
