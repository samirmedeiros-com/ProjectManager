using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;

namespace ProjectManagerWebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DebugController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public DebugController(ApplicationDbContext context) { _context = context; }


    [HttpGet("headers")]
    public IActionResult GetHeaders()
    {
        var headers = new Dictionary<string, string>();
        foreach (var header in Request.Headers)
        {
            headers[header.Key] = header.Value.ToString();
        }

        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            path = Request.Path,
            method = Request.Method,
            headers = headers,
            hasAuthorizationHeader = Request.Headers.ContainsKey("Authorization"),
            authorizationValue = Request.Headers["Authorization"].ToString()
        });
    }

    [HttpGet("protected")]
    [Authorize]
    public IActionResult GetProtected()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new
        {
            message = "✅ You are authenticated!",
            claims = claims
        });
    }
}
