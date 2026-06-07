using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;

namespace ProjectManagerWebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SetorController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SetorController(ApplicationDbContext context)
    {
        _context = context;
    }

    private string? GetUserRole()
    {
        return User.FindFirst("role")?.Value ?? User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
    }

    [AllowAnonymous]
    [HttpOptions("{*path}")]
    public IActionResult PreflightHandler()
    {
        return Ok();
    }

    [HttpGet("user-available")]
    public async Task<ActionResult<List<SetorDto>>> GetUserAvailableSectors()
    {
        var userIdString = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "0";

        if (!int.TryParse(userIdString, out var userId) || userId == 0)
            return Unauthorized();

        var userRole = GetUserRole();

        // Admin can see all sectors
        if (userRole == "Admin")
        {
            var allSetores = await _context.Setores.ToListAsync();
            return Ok(allSetores.Select(s => new SetorDto { Id = s.Id, Name = s.Name }).ToList());
        }

        // Gestors see only their sectors
        if (userRole == "Gestor")
        {
            var userSetores = await _context.UserSetores
                .Where(us => us.UserId == userId)
                .Include(us => us.Setor)
                .Select(us => us.Setor)
                .ToListAsync();

            return Ok(userSetores.Select(s => new SetorDto { Id = s.Id, Name = s.Name }).ToList());
        }

        // Other users cannot select sectors when creating projects
        return Ok(new List<SetorDto>());
    }
}
