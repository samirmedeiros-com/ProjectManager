using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

[Route("api/seur/tabelas")]
[ApiController]
[Authorize]
public class SeurTabelasController : ControllerBase
{
    private readonly ISeurTabelasService _service;

    public SeurTabelasController(ISeurTabelasService service)
    {
        _service = service;
    }

    // ---- CPOSTAL ----

    [HttpGet("cpostal")]
    public async Task<ActionResult<object>> GetCpostais([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var items = await _service.GetCpostaisAsync(search, page, pageSize);
        var total = await _service.CountCpostaisAsync(search);
        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize) });
    }

    [HttpGet("cpostal/{idt}")]
    public async Task<ActionResult<SeurCpostalDto>> GetCpostal(long idt)
    {
        var result = await _service.GetCpostalAsync(idt);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("cpostal")]
    public async Task<ActionResult<SeurCpostalDto>> CreateCpostal([FromBody] SaveCpostalDto dto)
        => Ok(await _service.CreateCpostalAsync(dto));

    [HttpPut("cpostal/{idt}")]
    public async Task<ActionResult<SeurCpostalDto>> UpdateCpostal(long idt, [FromBody] SaveCpostalDto dto)
    {
        var result = await _service.UpdateCpostalAsync(idt, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("cpostal/{idt}")]
    public async Task<IActionResult> DeleteCpostal(long idt)
    {
        var ok = await _service.DeleteCpostalAsync(idt);
        return ok ? Ok(new { message = "Eliminado com sucesso" }) : NotFound();
    }

    // ---- DESTINOS ----

    [HttpGet("destinos")]
    public async Task<ActionResult<object>> GetDestinos([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var items = await _service.GetDestinosAsync(search, page, pageSize);
        var total = await _service.CountDestinosAsync(search);
        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize) });
    }

    [HttpGet("destinos/{idt}")]
    public async Task<ActionResult<SeurDestinoDto>> GetDestino(long idt)
    {
        var result = await _service.GetDestinoAsync(idt);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("destinos")]
    public async Task<ActionResult<SeurDestinoDto>> CreateDestino([FromBody] SaveDestinoDto dto)
        => Ok(await _service.CreateDestinoAsync(dto));

    [HttpPut("destinos/{idt}")]
    public async Task<ActionResult<SeurDestinoDto>> UpdateDestino(long idt, [FromBody] SaveDestinoDto dto)
    {
        var result = await _service.UpdateDestinoAsync(idt, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("destinos/{idt}")]
    public async Task<IActionResult> DeleteDestino(long idt)
    {
        var ok = await _service.DeleteDestinoAsync(idt);
        return ok ? Ok(new { message = "Eliminado com sucesso" }) : NotFound();
    }

    // ---- PRODUCT ----

    [HttpGet("products")]
    public async Task<ActionResult<List<SeurProductDto>>> GetProducts([FromQuery] string? search)
        => Ok(await _service.GetProductsAsync(search));

    [HttpGet("products/{idt}")]
    public async Task<ActionResult<SeurProductDto>> GetProduct(long idt)
    {
        var result = await _service.GetProductAsync(idt);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("products")]
    public async Task<ActionResult<SeurProductDto>> CreateProduct([FromBody] SaveProductDto dto)
        => Ok(await _service.CreateProductAsync(dto));

    [HttpPut("products/{idt}")]
    public async Task<ActionResult<SeurProductDto>> UpdateProduct(long idt, [FromBody] SaveProductDto dto)
    {
        var result = await _service.UpdateProductAsync(idt, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("products/{idt}")]
    public async Task<IActionResult> DeleteProduct(long idt)
    {
        var ok = await _service.DeleteProductAsync(idt);
        return ok ? Ok(new { message = "Eliminado com sucesso" }) : NotFound();
    }

    // ---- SERVICE ----

    [HttpGet("services")]
    public async Task<ActionResult<List<SeurServiceDto>>> GetServices([FromQuery] string? search)
        => Ok(await _service.GetServicesAsync(search));

    [HttpGet("services/{idt}")]
    public async Task<ActionResult<SeurServiceDto>> GetService(long idt)
    {
        var result = await _service.GetServiceAsync(idt);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("services")]
    public async Task<ActionResult<SeurServiceDto>> CreateService([FromBody] SaveServiceDto dto)
        => Ok(await _service.CreateServiceAsync(dto));

    [HttpPut("services/{idt}")]
    public async Task<ActionResult<SeurServiceDto>> UpdateService(long idt, [FromBody] SaveServiceDto dto)
    {
        var result = await _service.UpdateServiceAsync(idt, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("services/{idt}")]
    public async Task<IActionResult> DeleteService(long idt)
    {
        var ok = await _service.DeleteServiceAsync(idt);
        return ok ? Ok(new { message = "Eliminado com sucesso" }) : NotFound();
    }

    // ---- CWENT_NUM ----

    [HttpGet("cwent")]
    public async Task<ActionResult<object>> GetCwents([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var items = await _service.GetCwentsAsync(search, page, pageSize);
        var total = await _service.CountCwentsAsync(search);
        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize) });
    }

    [HttpGet("cwent/{account}")]
    public async Task<ActionResult<CwentNumDto>> GetCwent(string account)
    {
        var result = await _service.GetCwentAsync(account);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("cwent")]
    public async Task<ActionResult<CwentNumDto>> CreateCwent([FromBody] CreateCwentNumDto dto)
    {
        try { return Ok(await _service.CreateCwentAsync(dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("cwent/{account}")]
    public async Task<ActionResult<CwentNumDto>> UpdateCwent(string account, [FromBody] SaveCwentNumDto dto)
    {
        var result = await _service.UpdateCwentAsync(account, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("cwent/{account}")]
    public async Task<IActionResult> DeleteCwent(string account)
    {
        var ok = await _service.DeleteCwentAsync(account);
        return ok ? Ok(new { message = "Eliminado com sucesso" }) : NotFound();
    }
}
