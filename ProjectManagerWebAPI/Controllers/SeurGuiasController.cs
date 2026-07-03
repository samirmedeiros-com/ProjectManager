using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

[Route("api/seur/guias")]
[ApiController]
[Authorize]
public class SeurGuiasController : ControllerBase
{
    private readonly ISeurGuiaService _seurGuiaService;

    public SeurGuiasController(ISeurGuiaService seurGuiaService)
    {
        _seurGuiaService = seurGuiaService;
    }

    [HttpGet("totais")]
    public async Task<ActionResult<SeurTotaisDto>> GetTotais([FromQuery] DateTime? data)
    {
        var dt = data ?? DateTime.Today;
        var result = await _seurGuiaService.GetTotaisByDataAsync(dt);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<SeurGuiaListDto>>> GetGuias(
        [FromQuery] string? guia,
        [FromQuery] string? referencia,
        [FromQuery] DateTime? data,
        [FromQuery] string? flagAtlas)
    {
        var result = await _seurGuiaService.GetGuiasAsync(guia, referencia, data, flagAtlas);
        return Ok(result);
    }

    [HttpGet("{idt}")]
    public async Task<ActionResult<SeurGuiaDetailDto>> GetGuia(long idt)
    {
        var result = await _seurGuiaService.GetGuiaByIdtAsync(idt);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{idt}")]
    public async Task<IActionResult> UpdateGuia(long idt, [FromBody] UpdateGuiaDto dto)
    {
        var success = await _seurGuiaService.UpdateGuiaAsync(idt, dto);
        if (!success) return NotFound();
        return Ok(new { message = "Guia atualizada com sucesso" });
    }

    [HttpPut("{idt}/flagatlas")]
    public async Task<IActionResult> UpdateFlagAtlas(long idt, [FromBody] UpdateFlagAtlasDto dto)
    {
        var validFlags = new[] { "N", "Y", "E", "X" };
        if (!validFlags.Contains(dto.FlagAtlas))
            return BadRequest(new { message = "FlagAtlas inválido. Use N, Y, E ou X." });

        var success = await _seurGuiaService.UpdateFlagAtlasAsync(idt, dto.FlagAtlas);
        if (!success) return NotFound();
        return Ok(new { message = "Status atualizado com sucesso" });
    }

    [HttpGet("{idt}/erros")]
    public async Task<ActionResult<List<SeurErroDto>>> GetErros(long idt, [FromQuery] string referencia)
    {
        var result = await _seurGuiaService.GetErrosByReferenciaAsync(referencia);
        return Ok(result);
    }

    [HttpGet("{idt}/parcels")]
    public async Task<ActionResult<List<SeurParcelDto>>> GetParcels(long idt, [FromQuery] string guia)
    {
        var result = await _seurGuiaService.GetParcelsByGuiaAsync(guia);
        return Ok(result);
    }

    [HttpGet("{idt}/verify")]
    public async Task<ActionResult<List<SeurVerifyDto>>> GetVerify(long idt, [FromQuery] string guia)
    {
        var result = await _seurGuiaService.GetVerifyByGuiaAsync(guia);
        return Ok(result);
    }

    [HttpPut("verify/{verifyIdt}/flag")]
    public async Task<IActionResult> UpdateVerifyFlag(long verifyIdt, [FromBody] UpdateVerifyFlagDto dto)
    {
        var validFlags = new[] { "N", "Y" };
        if (!validFlags.Contains(dto.VerifyFlag))
            return BadRequest(new { message = "VerifyFlag inválido. Use N ou Y." });

        var success = await _seurGuiaService.UpdateVerifyFlagAsync(verifyIdt, dto.VerifyFlag);
        if (!success) return NotFound();
        return Ok(new { message = "Flag de verificação atualizada" });
    }
}
