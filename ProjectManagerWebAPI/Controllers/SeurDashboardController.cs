using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectManagerWebAPI.Controllers;

[Route("api/seur/dashboard")]
[ApiController]
[Authorize]
public class SeurDashboardController : ControllerBase
{
    [HttpGet]
    public IActionResult GetDashboard()
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        var userName = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

        return Ok(new
        {
            message = "Bem-vindo ao Gestão SEUR",
            user = userName,
            email = userEmail,
            timestamp = DateTime.UtcNow
        });
    }
}
