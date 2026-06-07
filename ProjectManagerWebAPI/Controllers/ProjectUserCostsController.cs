using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

[ApiController]
[Route("api/projects/{projectId}/user-costs")]
[Authorize]
public class ProjectUserCostsController : ControllerBase
{
    private readonly ProjectUserCostService _costService;

    public ProjectUserCostsController(ProjectUserCostService costService)
    {
        _costService = costService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectUserCostDto>>> GetCostsByProject(int projectId)
    {
        try
        {
            var costs = await _costService.GetCostsByProjectAsync(projectId);
            return Ok(costs);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<ProjectUserCostDto>> GetCost(int projectId, int userId)
    {
        try
        {
            var cost = await _costService.GetCostAsync(projectId, userId);
            if (cost == null)
                return NotFound(new { message = "Custo não encontrado" });

            return Ok(cost);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{userId}")]
    public async Task<ActionResult<ProjectUserCostDto>> CreateOrUpdateCost(int projectId, int userId, [FromBody] CreateProjectUserCostRequest request)
    {
        try
        {
            if (request.CostPerHour < 0)
                return BadRequest(new { message = "Custo por hora deve ser maior ou igual a 0" });

            var cost = await _costService.CreateOrUpdateCostAsync(projectId, userId, request.CostPerHour);
            return Ok(cost);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{userId}")]
    public async Task<ActionResult> DeleteCost(int projectId, int userId)
    {
        try
        {
            var deleted = await _costService.DeleteCostAsync(projectId, userId);
            if (!deleted)
                return NotFound(new { message = "Custo não encontrado" });

            return Ok(new { message = "Custo removido com sucesso" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class CreateProjectUserCostRequest
{
    public decimal CostPerHour { get; set; }
}
