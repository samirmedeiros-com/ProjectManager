using ProjectManagerWebAPI.Models;
using ProjectManagerWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectManagerWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimesheetsController : ControllerBase
{
    private readonly TimesheetService _timesheetService;

    public TimesheetsController(TimesheetService timesheetService)
    {
        _timesheetService = timesheetService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        throw new UnauthorizedAccessException("Usuário não autenticado");
    }

    [HttpPost("criar")]
    public async Task<ActionResult<TimesheetResponse>> CreateTimesheet([FromBody] CreateTimesheetRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var timesheet = await _timesheetService.CreateTimesheetAsync(
                request.ProjectId,
                request.UserId,
                request.WeekStartDate,
                userId
            );

            var response = _timesheetService.GetTimesheetResponse(await _timesheetService.GetTimesheetByIdAsync(timesheet.Id) ?? timesheet);
            return CreatedAtAction(nameof(GetTimesheet), new { id = timesheet.Id }, response);
        }
        catch (InvalidOperationException ex)
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

    [HttpGet("meus-timesheets")]
    public async Task<ActionResult<List<TimesheetListResponse>>> GetMyTimesheets()
    {
        try
        {
            var userId = GetCurrentUserId();
            var timesheets = await _timesheetService.GetUserTimesheetsAsync(userId);
            return Ok(_timesheetService.GetTimesheetListResponse(timesheets));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("projeto/{projectId}")]
    public async Task<ActionResult<List<TimesheetListResponse>>> GetTimesheetsByProject(int projectId)
    {
        try
        {
            var timesheets = await _timesheetService.GetTimesheetsByProjectAsync(projectId);
            return Ok(_timesheetService.GetTimesheetListResponse(timesheets));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/horas")]
    public async Task<ActionResult<TimesheetResponse>> UpdateEntries(int id, [FromBody] UpdateTimesheetEntriesRequest request)
    {
        try
        {
            var timesheet = await _timesheetService.UpdateEntriesAsync(id, request.Entries);
            var updated = await _timesheetService.GetTimesheetByIdAsync(id);
            return Ok(_timesheetService.GetTimesheetResponse(updated ?? timesheet));
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

    [HttpPut("{id}/submeter")]
    public async Task<ActionResult<TimesheetResponse>> SubmitTimesheet(int id, [FromBody] SubmitTimesheetRequest? request = null)
    {
        try
        {
            var timesheet = await _timesheetService.SubmitTimesheetAsync(id);
            var updated = await _timesheetService.GetTimesheetByIdAsync(id);
            return Ok(_timesheetService.GetTimesheetResponse(updated ?? timesheet));
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
    public async Task<ActionResult<TimesheetResponse>> ApproveTimesheet(int id, [FromBody] ApproveTimesheetRequest? request = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var timesheet = await _timesheetService.ApproveTimesheetAsync(id, userId);
            var updated = await _timesheetService.GetTimesheetByIdAsync(id);
            return Ok(_timesheetService.GetTimesheetResponse(updated ?? timesheet));
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
            var updated = await _timesheetService.GetTimesheetByIdAsync(id);
            return Ok(_timesheetService.GetTimesheetResponse(updated ?? timesheet));
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
