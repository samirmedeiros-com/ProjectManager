using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Services;
using System.Security.Claims;

namespace ProjectManagerWebAPI.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummaryDto>> GetReportSummary([FromQuery] int? monthOffset = null)
    {
        try
        {
            var summary = await _reportService.GetReportSummaryAsync(monthOffset);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("hours-by-month")]
    public async Task<ActionResult<List<HoursByMonthDto>>> GetHoursByMonth([FromQuery] int? userId = null)
    {
        try
        {
            var hours = await _reportService.GetHoursByMonthAsync(userId);
            return Ok(hours);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("hours-by-project")]
    public async Task<ActionResult<List<HoursByProjectWithCostDto>>> GetHoursByProject([FromQuery] int? userId = null, [FromQuery] int? monthOffset = null)
    {
        try
        {
            var hours = await _reportService.GetHoursByProjectWithCostAsync(userId, monthOffset);
            return Ok(hours);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("hours-by-user")]
    public async Task<ActionResult<List<HoursByUserWithCostDto>>> GetHoursByUser([FromQuery] int? monthOffset = null)
    {
        try
        {
            var hours = await _reportService.GetHoursByUserWithCostAsync(monthOffset);
            return Ok(hours);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
