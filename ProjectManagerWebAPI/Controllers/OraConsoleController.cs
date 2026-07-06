using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

[Route("api/oraconsole")]
[ApiController]
[Authorize]
public class OraConsoleController : ControllerBase
{
    private readonly IOraConsoleSchemaService _schemaService;
    private readonly IOraConsoleQueryService _queryService;

    public OraConsoleController(IOraConsoleSchemaService schemaService, IOraConsoleQueryService queryService)
    {
        _schemaService = schemaService;
        _queryService = queryService;
    }

    private string? SessionId => User.FindFirst("sub")?.Value;
    private bool IsOraConsoleSession => User.FindFirst("app")?.Value == "oraconsole";

    [HttpGet("schemas")]
    public async Task<IActionResult> GetSchemas()
    {
        if (!IsOraConsoleSession || SessionId == null) return Forbid();
        try
        {
            return Ok(await _schemaService.GetSchemasAsync(SessionId));
        }
        catch (OraConsoleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("schemas/{owner}/tables")]
    public async Task<IActionResult> GetTables(string owner)
    {
        if (!IsOraConsoleSession || SessionId == null) return Forbid();
        try
        {
            return Ok(await _schemaService.GetTablesAsync(SessionId, owner));
        }
        catch (OraConsoleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("schemas/{owner}/tables/{tableName}/columns")]
    public async Task<IActionResult> GetColumns(string owner, string tableName)
    {
        if (!IsOraConsoleSession || SessionId == null) return Forbid();
        try
        {
            return Ok(await _schemaService.GetColumnsAsync(SessionId, owner, tableName));
        }
        catch (OraConsoleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("query/execute")]
    public async Task<IActionResult> Execute([FromBody] OraConsoleQueryRequest request)
    {
        if (!IsOraConsoleSession || SessionId == null) return Forbid();
        try
        {
            return Ok(await _queryService.ExecuteAsync(SessionId, request));
        }
        catch (OraConsoleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("cell")]
    public async Task<IActionResult> UpdateCell([FromBody] OraConsoleCellUpdateRequest request)
    {
        if (!IsOraConsoleSession || SessionId == null) return Forbid();
        try
        {
            return Ok(await _queryService.UpdateCellAsync(SessionId, request));
        }
        catch (OraConsoleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
