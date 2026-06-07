using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserPermissionService _permissionService;

    public UsersController(IUserService userService, IUserPermissionService permissionService)
    {
        _userService = userService;
        _permissionService = permissionService;
    }

    private int GetCurrentUserId()
    {
        // Procura por "sub" ou NameIdentifier no JWT
        foreach (var claimType in new[] { "sub", System.Security.Claims.ClaimTypes.NameIdentifier })
        {
            var claim = User.FindFirst(claimType);
            if (claim != null && int.TryParse(claim.Value, out var id) && id > 0)
                return id;
        }

        return 0;
    }

    private string GetCurrentUserRole()
    {
        // Tenta encontrar a claim "role" (customizada)
        var claim = User.FindFirst("role");
        if (claim != null)
            return claim.Value;

        // Se não encontrou, tenta o tipo padrão
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
        if (roleClaim != null)
            return roleClaim.Value;

        // Default
        return "Utilizador";
    }

    [AllowAnonymous]
    [HttpOptions("{*path}")]
    public IActionResult PreflightHandler()
    {
        return Ok();
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole != "Admin" && currentUserRole != "Gestor")
            return Forbid();

        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole != "Admin" && currentUserRole != "Gestor")
            return Forbid();

        // Se currentUserId é 0, ele não foi extraído corretamente do token
        if (currentUserId == 0)
            return BadRequest("Não foi possível extrair o ID do usuário do token");

        var users = await _userService.GetAllUsersAsync(currentUserId);
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole == "Gestor")
        {
            var canManage = await _permissionService.CanManageUser(currentUserId, id);
            if (!canManage)
                return Forbid();
        }

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole != "Admin" && currentUserRole != "Gestor")
            return Forbid();

        try
        {
            var user = await _userService.UpdateUserAsync(id, request, currentUserId, currentUserRole);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole != "Admin")
            return Forbid();

        var success = await _userService.DeleteUserAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<ActionResult<UserDto>> Deactivate(int id)
    {
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole != "Admin" && currentUserRole != "Gestor")
            return Forbid();

        var user = await _userService.DeactivateUserAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPatch("{id}/activate")]
    public async Task<ActionResult<UserDto>> Activate(int id)
    {
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole != "Admin" && currentUserRole != "Gestor")
            return Forbid();

        var user = await _userService.ActivateUserAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPost("{id}/setores")]
    public async Task<ActionResult<UserDto>> AssignSetores(int id, AssignSetoresRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole != "Admin" && currentUserRole != "Gestor")
            return Forbid();

        if (currentUserRole == "Gestor")
        {
            var allowedSetores = await _permissionService.GetAllowedSetoresForUser(currentUserId);
            foreach (var setorId in request.SetorIds)
            {
                if (!allowedSetores.Contains(setorId))
                    return Forbid();
            }
        }

        var user = await _userService.AssignSetoresAsync(id, request.SetorIds);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpGet("{id}/setores")]
    public async Task<ActionResult<List<SetorDto>>> GetUserSetores(int id)
    {
        var setores = await _userService.GetUserSetoresAsync(id);
        if (setores == null)
            return NotFound();

        return Ok(setores);
    }

    [HttpDelete("{id}/setores/{setorId}")]
    public async Task<IActionResult> RemoveSetor(int id, int setorId)
    {
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole != "Admin")
            return Forbid();

        var success = await _userService.RemoveSetorAsync(id, setorId);
        if (!success)
            return NotFound();

        return NoContent();
    }
}
