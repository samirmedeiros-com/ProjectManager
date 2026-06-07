using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services;

public class ProjectHistoryDto
{
    public string Type { get; set; } = "";
    public string Action { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime Date { get; set; }
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime? ChangedAt { get; set; }
}

public interface ICommentService
{
    Task<List<CommentDto>> GetProjectComments(int projectId);
    Task<CommentDto> CreateComment(int projectId, string authorEmail, CreateCommentRequest request);
    Task<List<ProjectHistoryDto>> GetProjectHistory(int projectId);
}

public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _context;

    public CommentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CommentDto>> GetProjectComments(int projectId)
    {
        var comments = await _context.Comments
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(c => new CommentDto
        {
            Id = c.Id,
            ProjectId = c.ProjectId,
            Content = c.Content,
            Author = c.Author,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<CommentDto> CreateComment(int projectId, string authorEmail, CreateCommentRequest request)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
            throw new InvalidOperationException("Projeto não encontrado");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == authorEmail);
        var authorName = user?.FullName ?? authorEmail;

        var comment = new Comment
        {
            ProjectId = projectId,
            Content = request.Content,
            Author = authorName,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return new CommentDto
        {
            Id = comment.Id,
            ProjectId = comment.ProjectId,
            Content = comment.Content,
            Author = comment.Author,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<List<ProjectHistoryDto>> GetProjectHistory(int projectId)
    {
        var history = new List<ProjectHistoryDto>();

        // Adicionar evento de criação do projeto
        var project = await _context.Projects.FindAsync(projectId);
        if (project != null)
        {
            history.Add(new ProjectHistoryDto
            {
                Type = "created",
                Action = "Projeto criado",
                Author = "Sistema",
                Date = project.CreatedAt
            });
        }

        // Adicionar histórico de status
        var statusHistory = await _context.ProjectStatusHistories
            .Where(sh => sh.ProjectId == projectId)
            .OrderBy(sh => sh.ChangedAt)
            .ToListAsync();

        foreach (var status in statusHistory)
        {
            var authorName = GetManagerName(status.ChangedBy ?? "Sistema");
            history.Add(new ProjectHistoryDto
            {
                Type = "status",
                Action = $"Status alterado de {TranslateStatus(status.FromStatus)} para {TranslateStatus(status.ToStatus)}",
                Author = authorName,
                Date = status.ChangedAt,
                FromStatus = status.FromStatus,
                ToStatus = status.ToStatus,
                ChangedBy = authorName,
                ChangedAt = status.ChangedAt
            });
        }

        // Adicionar histórico de mudança de manager/responsável
        var managerHistory = await _context.ProjectManagerHistories
            .Where(mh => mh.ProjectId == projectId)
            .ToListAsync();

        foreach (var manager in managerHistory)
        {
            var fromManagerName = GetManagerName(manager.FromManager);
            var toManagerName = GetManagerName(manager.ToManager);
            var changedByName = GetManagerName(manager.ChangedBy ?? "Sistema");

            history.Add(new ProjectHistoryDto
            {
                Type = "manager",
                Action = $"Responsável alterado de {fromManagerName} para {toManagerName}",
                Author = changedByName,
                Date = manager.ChangedAt
            });
        }

        // Adicionar histórico de mudança de owner
        var ownerHistory = await _context.ProjectOwnerHistories
            .Where(oh => oh.ProjectId == projectId)
            .ToListAsync();

        foreach (var owner in ownerHistory)
        {
            var changedByName = GetManagerName(owner.ChangedBy ?? "Sistema");

            history.Add(new ProjectHistoryDto
            {
                Type = "owner",
                Action = $"Owner alterado de {owner.FromOwner} para {owner.ToOwner}",
                Author = changedByName,
                Date = owner.ChangedAt
            });
        }

        // Adicionar comentários
        var comments = await _context.Comments
            .Where(c => c.ProjectId == projectId)
            .ToListAsync();

        foreach (var comment in comments)
        {
            history.Add(new ProjectHistoryDto
            {
                Type = "comment",
                Action = "Comentário adicionado",
                Author = comment.Author ?? "Desconhecido",
                Date = comment.CreatedAt
            });
        }

        // Ordenar por data ascendente (para manter sequência correta na evolução)
        return history.OrderBy(h => h.Date).ToList();
    }

    private string TranslateStatus(string status)
    {
        return status switch
        {
            "Planning" => "Planejamento",
            "Development" => "Desenvolvimento",
            "Completed" => "Concluído",
            "On Hold" => "Em Espera",
            "Released" => "Liberado",
            "Finished" => "Finalizado",
            _ => status
        };
    }

    private string GetManagerName(string email)
    {
        if (string.IsNullOrEmpty(email) || email == "Desconhecido" || email == "Sistema")
            return email;

        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        return user?.FullName ?? email;
    }
}
