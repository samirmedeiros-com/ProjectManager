using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SetoresController : ControllerBase
{
    private readonly ISetorService _setorService;

    public SetoresController(ISetorService setorService)
    {
        _setorService = setorService;
    }

    [HttpGet]
    public async Task<ActionResult<List<SetorDto>>> GetAll()
    {
        var setores = await _setorService.GetAllSetoresAsync();
        return Ok(setores);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SetorDto>> GetById(int id)
    {
        var setor = await _setorService.GetSetorByIdAsync(id);
        if (setor == null)
            return NotFound();

        return Ok(setor);
    }

    [HttpPost]
    public async Task<ActionResult<SetorDto>> Create(CreateSetorRequest request)
    {
        var setor = await _setorService.CreateSetorAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = setor.Id }, setor);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SetorDto>> Update(int id, UpdateSetorRequest request)
    {
        var setor = await _setorService.UpdateSetorAsync(id, request);
        if (setor == null)
            return NotFound();

        return Ok(setor);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _setorService.DeleteSetorAsync(id);
        if (!success)
            return BadRequest("Setor não pode ser deletado pois contém utilizadores.");

        return NoContent();
    }
}
