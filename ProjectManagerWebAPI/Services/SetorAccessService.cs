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

        // O IsActive é um bool mapeado para NUMBER(1): usá-lo como predicado faz o provider
        // Oracle rebentar a gerar o literal booleano do SQL
        // (OracleBoolTypeMapping.GenerateNonNullSqlLiteral). Por isso a tabela — que é
        // pequena — é lida e filtrada em memória. É o mesmo contorno que o
        // UserPermissionService já fazia ao evitar IsActive nas consultas.
        var setores = await _context.Setores
            .Select(s => new { s.Id, s.Name, s.IsActive })
            .ToListAsync();

        var ids = setores
            .Where(s => s.IsActive && string.Equals(s.Name, setor, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Id)
            .ToList();

        if (ids.Count == 0)
            return false;

        // CountAsync e não AnyAsync: o provider Oracle traduz o Any() para
        // "CASE WHEN EXISTS (...) THEN True ELSE False END", e o Oracle não tem literais
        // booleanos antes da 23c — rebenta com ORA-00904: "FALSE": identificador inválido.
        var associacoes = await _context.UserSetores
            .CountAsync(us => us.UserId == userId && ids.Contains(us.SetorId));

        return associacoes > 0;
    }
}
