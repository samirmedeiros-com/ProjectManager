using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;

namespace ProjectManagerWebAPI.Services;

public interface ISetorAccessService
{
    /// <summary>Diz se o utilizador pertence a um setor ativo com este nome (sem distinguir maiúsculas).</summary>
    Task<bool> PertenceAoSetorAsync(int userId, string setor);
}

public class SetorAccessService : ISetorAccessService
{
    private readonly ApplicationDbContext _context;

    public SetorAccessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> PertenceAoSetorAsync(int userId, string setor)
    {
        // O setor não vai no JWT (o token só traz sub/email/name/role), por isso a pertença
        // tem sempre de ser confirmada contra a base de dados.
        var nome = setor.ToUpper();

        return await _context.UserSetores
            .AnyAsync(us => us.UserId == userId
                         && us.Setor.IsActive
                         && us.Setor.Name.ToUpper() == nome);
    }
}
