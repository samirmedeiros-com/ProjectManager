using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagerWebAPI.Services;

public interface ISetorService
{
    Task<List<SetorDto>> GetAllSetoresAsync();
    Task<SetorDto?> GetSetorByIdAsync(int id);
    Task<SetorDto> CreateSetorAsync(CreateSetorRequest request);
    Task<SetorDto?> UpdateSetorAsync(int id, UpdateSetorRequest request);
    Task<bool> DeleteSetorAsync(int id);
}

public class SetorService : ISetorService
{
    private readonly ApplicationDbContext _context;

    public SetorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SetorDto>> GetAllSetoresAsync()
    {
        var setores = await _context.Setores
            .OrderBy(s => s.Name)
            .ToListAsync();

        return setores.Select(MapToDto).ToList();
    }

    public async Task<SetorDto?> GetSetorByIdAsync(int id)
    {
        var setor = await _context.Setores.FindAsync(id);
        return setor == null ? null : MapToDto(setor);
    }

    public async Task<SetorDto> CreateSetorAsync(CreateSetorRequest request)
    {
        var setor = new Setor
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Setores.Add(setor);
        await _context.SaveChangesAsync();

        return MapToDto(setor);
    }

    public async Task<SetorDto?> UpdateSetorAsync(int id, UpdateSetorRequest request)
    {
        var setor = await _context.Setores.FindAsync(id);
        if (setor == null) return null;

        setor.Name = request.Name;
        setor.Description = request.Description;
        setor.IsActive = request.IsActive;
        setor.UpdatedAt = DateTime.UtcNow;

        _context.Setores.Update(setor);
        await _context.SaveChangesAsync();

        return MapToDto(setor);
    }

    public async Task<bool> DeleteSetorAsync(int id)
    {
        var setor = await _context.Setores.FindAsync(id);
        if (setor == null) return false;

        var usersInSetor = await _context.UserSetores.CountAsync(us => us.SetorId == id);
        if (usersInSetor > 0) return false;

        _context.Setores.Remove(setor);
        await _context.SaveChangesAsync();

        return true;
    }

    private static SetorDto MapToDto(Setor setor)
    {
        return new SetorDto
        {
            Id = setor.Id,
            Name = setor.Name,
            Description = setor.Description,
            IsActive = setor.IsActive,
            CreatedAt = setor.CreatedAt,
            UpdatedAt = setor.UpdatedAt
        };
    }
}
