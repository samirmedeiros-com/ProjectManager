using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

[Route("api/oraconsole/auth")]
[ApiController]
public class OraConsoleAuthController : ControllerBase
{
    private readonly IOraConsoleAuthService _authService;

    public OraConsoleAuthController(IOraConsoleAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] OraConsoleLoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        if (!response.Success)
            return Unauthorized(response);

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var sessionId = User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(sessionId))
            _authService.Logout(sessionId);

        return Ok(new { message = "Sessão terminada" });
    }
}
