using ProjectManagerWebAPI.Models;
using ProjectManagerWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ProjectManagerWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimesheetsController : ControllerBase
{
    private readonly TimesheetService _timesheetService;
    private readonly ILogger<TimesheetsController> _logger;

    public TimesheetsController(TimesheetService timesheetService, ILogger<TimesheetsController> logger)
    {
        _timesheetService = timesheetService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        if (int.TryParse(userIdClaim, out var userId))
            return userId;

        throw new UnauthorizedAccessException("Usuário não autenticado");
    }

    [HttpGet("dia/{date}")]
    public async Task<ActionResult<TimesheetResponse>> GetDayTimesheet(string date)
    {
        try
        {
            if (!DateTime.TryParse(date, out var parsedDate))
                return BadRequest(new { message = "Data inválida" });

            var userId = GetCurrentUserId();
            var timesheet = await _timesheetService.GetOrCreateDailyTimesheetAsync(userId, parsedDate);
            return Ok(_timesheetService.GetTimesheetResponse(timesheet));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TimesheetResponse>> GetTimesheet(int id)
    {
        try
        {
            var timesheet = await _timesheetService.GetTimesheetByIdAsync(id);
            if (timesheet == null)
                return NotFound(new { message = "Timesheet não encontrado" });

            return Ok(_timesheetService.GetTimesheetResponse(timesheet));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("meus")]
    public async Task<ActionResult<List<TimesheetListResponse>>> GetMyTimesheets([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = GetCurrentUserId();
            var timesheets = await _timesheetService.GetUserTimesheetsAsync(userId, startDate, endDate);
            return Ok(_timesheetService.GetTimesheetListResponse(timesheets));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/projeto/{projectId}")]
    public async Task<ActionResult<TimesheetResponse>> AddProjectEntry(int id, int projectId, [FromBody] AddProjectEntryRequest request)
    {
        try
        {
            if (request.WorkHours < 0 || request.WorkHours > 24)
                return BadRequest(new { message = "Horas devem estar entre 0 e 24" });

            var timesheet = await _timesheetService.AddProjectEntryAsync(id, projectId, request.WorkHours, request.Notes);
            return Ok(_timesheetService.GetTimesheetResponse(timesheet));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}/projeto/{projectId}")]
    public async Task<ActionResult<TimesheetResponse>> RemoveProjectEntry(int id, int projectId)
    {
        try
        {
            var timesheet = await _timesheetService.RemoveProjectEntryAsync(id, projectId);
            return Ok(_timesheetService.GetTimesheetResponse(timesheet));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/submeter")]
    public async Task<ActionResult<TimesheetResponse>> SubmitTimesheet(int id)
    {
        try
        {
            var timesheet = await _timesheetService.SubmitTimesheetAsync(id);
            return Ok(_timesheetService.GetTimesheetResponse(timesheet));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/aprovar")]
    public async Task<ActionResult<TimesheetResponse>> ApproveTimesheet(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var timesheet = await _timesheetService.ApproveTimesheetAsync(id, userId);
            return Ok(_timesheetService.GetTimesheetResponse(timesheet));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/rejeitar")]
    public async Task<ActionResult<TimesheetResponse>> RejectTimesheet(int id, [FromBody] RejectTimesheetRequest request)
    {
        try
        {
            var timesheet = await _timesheetService.RejectTimesheetAsync(id, request.Reason);
            return Ok(_timesheetService.GetTimesheetResponse(timesheet));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTimesheet(int id)
    {
        try
        {
            await _timesheetService.DeleteTimesheetAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("pendentes-aprovacao/{setorId}")]
    public async Task<ActionResult<List<TimesheetListResponse>>> GetPendingApprovals(int setorId)
    {
        try
        {
            var timesheets = await _timesheetService.GetPendingApprovalsAsync(setorId);
            return Ok(_timesheetService.GetTimesheetListResponse(timesheets));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class AddProjectEntryRequest
{
    public decimal WorkHours { get; set; }
    public string? Notes { get; set; }
}
