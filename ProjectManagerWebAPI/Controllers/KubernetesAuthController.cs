using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Filters;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

/// <summary>
/// Login próprio da Gestão Kubernetes — como o SEUR e o OraConsole, e ao contrário do portal
/// de OpenSearch, esta aplicação não partilha credenciais com o Project Manager.
/// </summary>
[ApiController]
[Route("api/kubernetes/auth")]
public class KubernetesAuthController : ControllerBase
{
    private readonly IKubernetesAuthService _auth;
    private readonly ILogger<KubernetesAuthController> _logger;

    public KubernetesAuthController(IKubernetesAuthService auth, ILogger<KubernetesAuthController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    /// <summary>
    /// IP de quem faz o pedido, para o registo de ações. Atrás do ingress o endereço direto é o
    /// do proxy, por isso o X-Forwarded-For tem precedência quando existe.
    /// </summary>
    private string? Ip()
    {
        var encaminhado = Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(encaminhado))
            return encaminhado.Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    [HttpPost("login")]
    public async Task<ActionResult<K8sLoginResponse>> Login([FromBody] K8sLoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resposta = await _auth.LoginAsync(request, Ip());

        // Tentativas falhadas ficam no log: esta aplicação manda parar produção.
        if (!resposta.Success)
        {
            _logger.LogWarning("Gestão Kubernetes: login recusado para {Email}.", request.Email);
            return Unauthorized(resposta);
        }

        return Ok(resposta);
    }

    /// <summary>Quem está autenticado, para o guard do Angular confirmar a sessão no servidor.</summary>
    [HttpGet("acesso")]
    [Authorize]
    [RequerApp(KubernetesAuthService.Aplicacao)]
    public ActionResult<object> Acesso() => Ok(new
    {
        permitido = true,
        papel = User.FindFirst("role")?.Value,
        nome = User.FindFirst("name")?.Value
    });

    // ── Gestão de utilizadores (só Admin) ────────────────────────────────

    [HttpGet("users")]
    [Authorize]
    [RequerApp(KubernetesAuthService.Aplicacao)]
    [RequerPapel(KubernetesAuthService.PapelAdmin)]
    public async Task<ActionResult<List<K8sUserDetailDto>>> Users()
        => Ok(await _auth.GetAllUsersAsync());

    [HttpPost("users")]
    [Authorize]
    [RequerApp(KubernetesAuthService.Aplicacao)]
    [RequerPapel(KubernetesAuthService.PapelAdmin)]
    public async Task<ActionResult<CreateK8sUserResponseDto>> CreateUser([FromBody] CreateK8sUserDto dto)
    {
        try
        {
            return Ok(await _auth.CreateUserAsync(dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("users/{id:int}")]
    [Authorize]
    [RequerApp(KubernetesAuthService.Aplicacao)]
    [RequerPapel(KubernetesAuthService.PapelAdmin)]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        // Um Admin que se desative a si próprio deixa a aplicação sem quem a administre
        // se for o único — mais simples é impedir o caso todo.
        if (int.TryParse(User.FindFirst("sub")?.Value, out var proprio) && proprio == id)
            return BadRequest(new { message = "Não pode desativar a sua própria conta." });

        return await _auth.DeactivateUserAsync(id)
            ? Ok(new { message = "Utilizador desativado" })
            : NotFound();
    }

    [HttpDelete("users/{id:int}/definitivo")]
    [Authorize]
    [RequerApp(KubernetesAuthService.Aplicacao)]
    [RequerPapel(KubernetesAuthService.PapelAdmin)]
    public async Task<IActionResult> RemoverUser(int id)
    {
        if (int.TryParse(User.FindFirst("sub")?.Value, out var proprio) && proprio == id)
            return BadRequest(new { message = "Não pode remover a sua própria conta." });

        return await _auth.RemoverUserAsync(id)
            ? Ok(new { message = "Utilizador removido" })
            : NotFound();
    }

    [HttpPost("users/{id:int}/reset-password")]
    [Authorize]
    [RequerApp(KubernetesAuthService.Aplicacao)]
    [RequerPapel(KubernetesAuthService.PapelAdmin)]
    public async Task<ActionResult<ResetPasswordResponseDto>> ResetPassword(int id)
    {
        var resultado = await _auth.ResetPasswordAsync(id);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpPut("change-password")]
    [Authorize]
    [RequerApp(KubernetesAuthService.Aplicacao)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
            return BadRequest(new { message = "A nova password precisa de pelo menos 8 caracteres." });

        return await _auth.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword)
            ? Ok(new { message = "Password alterada com sucesso" })
            : BadRequest(new { message = "Password atual incorreta" });
    }
}
