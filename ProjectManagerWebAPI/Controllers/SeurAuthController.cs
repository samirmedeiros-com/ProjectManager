using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

[Route("api/seur/auth")]
[ApiController]
public class SeurAuthController : ControllerBase
{
    private readonly ISeurAuthService _seurAuthService;

    public SeurAuthController(ISeurAuthService seurAuthService)
    {
        _seurAuthService = seurAuthService;
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] SeurLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { success = false, message = "Email é obrigatório" });

        var (success, message) = await _seurAuthService.ForgotPasswordAsync(request.Email);
        return Ok(new { success, message });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] SeurLoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = _seurAuthService.Login(request);
        if (!response.Success)
            return Unauthorized(response);

        return Ok(response);
    }

    private bool IsAdmin() => User.FindFirst("role")?.Value == "Admin";

    [HttpGet("users")]
    [Authorize]
    public async Task<ActionResult<List<SeurUserDetailDto>>> GetAllUsers()
    {
        if (!IsAdmin()) return Forbid();
        var users = await _seurAuthService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost("users")]
    [Authorize]
    public async Task<ActionResult<CreateUserResponseDto>> CreateUser([FromBody] CreateSeurUserDto dto)
    {
        if (!IsAdmin()) return Forbid();
        try
        {
            var result = await _seurAuthService.CreateUserAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("users/{id}")]
    [Authorize]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        if (!IsAdmin()) return Forbid();
        var success = await _seurAuthService.DeactivateUserAsync(id);
        if (!success) return NotFound();
        return Ok(new { message = "Utilizador desativado" });
    }

    [HttpPost("users/{id}/reset-password")]
    [Authorize]
    public async Task<ActionResult<ResetPasswordResponseDto>> ResetPassword(int id)
    {
        if (!IsAdmin()) return Forbid();
        var result = await _seurAuthService.ResetPasswordAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdStr = User.FindFirst("sub")?.Value;
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var success = await _seurAuthService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
        if (!success) return BadRequest(new { message = "Password atual incorreta" });
        return Ok(new { message = "Password alterada com sucesso" });
    }
}
