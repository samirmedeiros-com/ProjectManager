using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services;

public interface IProjectService
{
    Task<List<ProjectDto>> GetAllProjects();
    Task<ProjectDto?> GetProjectById(int id);
    Task<ProjectDto> CreateProject(CreateProjectRequest request);
    Task<ProjectDto?> UpdateProject(int id, UpdateProjectRequest request);
    Task<ProjectDto?> UpdateProjectManager(int id, string manager, string? changedByEmail = null);
    Task<ProjectDto?> UpdateProjectOwner(int id, int? ownerId, string? changedByEmail = null);
    Task<ProjectDto?> UpdateProjectStatus(int id, string status, string? changedByEmail = null);
    Task<bool> DeleteProject(int id);
    Task<bool> AddProjectMember(int projectId, int userId, string role = "Member");
    Task<bool> RemoveProjectMember(int projectMemberId);
    Task<List<ProjectMemberDto>> GetProjectMembers(int projectId);
}

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;

    public ProjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProjectDto>> GetAllProjects()
    {
        var projects = await _context.Projects
            .Include(p => p.Owner)
            .Include(p => p.Setor)
            .Include(p => p.ProjectMembers)
            .ThenInclude(pm => pm.User)
            .Include(p => p.Tasks)
            .Include(p => p.Comments)
            .ToListAsync();

        return projects.Select(MapToDto).ToList();
    }

    public async Task<ProjectDto?> GetProjectById(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Owner)
            .Include(p => p.Setor)
            .Include(p => p.ProjectMembers)
            .ThenInclude(pm => pm.User)
            .Include(p => p.Tasks)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id);

        return project == null ? null : MapToDto(project);
    }

    public async Task<ProjectDto> CreateProject(CreateProjectRequest request)
    {
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Priority = request.Priority,
            Manager = request.Manager,
            OwnerId = request.OwnerId,
            SetorId = request.SetorId,
            FreshDeskId = request.FreshDeskId,
            Status = "Planning"
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Carregar o Owner e Setor para retornar com ownerName e setorName preenchidos
        return await GetProjectById(project.Id);
    }

    public async Task<ProjectDto?> UpdateProject(int id, UpdateProjectRequest request)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return null;

        if (!string.IsNullOrEmpty(request.Name)) project.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Description)) project.Description = request.Description;
        if (request.StartDate.HasValue) project.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) project.EndDate = request.EndDate.Value;
        if (!string.IsNullOrEmpty(request.Status)) project.Status = request.Status;
        if (request.Priority.HasValue) project.Priority = request.Priority.Value;
        if (request.OwnerId.HasValue) project.OwnerId = request.OwnerId.Value;
        if (request.SetorId.HasValue) project.SetorId = request.SetorId.Value;
        if (!string.IsNullOrEmpty(request.FreshDeskId)) project.FreshDeskId = request.FreshDeskId;

        project.UpdatedAt = DateTime.UtcNow;

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();

        return await GetProjectById(id);
    }

    public async Task<ProjectDto?> UpdateProjectManager(int id, string manager, string? changedByEmail = null)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return null;

        var oldManager = project.Manager ?? "Desconhecido";

        project.Manager = manager;
        project.UpdatedAt = DateTime.UtcNow;

        _context.Projects.Update(project);

        // Registrar histórico de mudança de manager
        var managerHistory = new ProjectManagerHistory
        {
            ProjectId = id,
            FromManager = oldManager,
            ToManager = manager,
            ChangedBy = changedByEmail ?? manager,
            ChangedAt = DateTime.UtcNow
        };
        _context.ProjectManagerHistories.Add(managerHistory);

        await _context.SaveChangesAsync();

        return await GetProjectById(id);
    }

    public async Task<ProjectDto?> UpdateProjectOwner(int id, int? ownerId, string? changedByEmail = null)
    {
        var project = await _context.Projects
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return null;

        var oldOwner = project.Owner?.FullName ?? "Sem atribuição";
        var newOwner = "Sem atribuição";

        if (ownerId.HasValue)
        {
            var owner = await _context.Users.FindAsync(ownerId.Value);
            if (owner != null)
                newOwner = owner.FullName;
        }

        project.OwnerId = ownerId;
        project.UpdatedAt = DateTime.UtcNow;

        _context.Projects.Update(project);

        // Registrar histórico de mudança de owner
        var ownerHistory = new ProjectOwnerHistory
        {
            ProjectId = id,
            FromOwner = oldOwner,
            ToOwner = newOwner,
            ChangedBy = changedByEmail ?? newOwner,
            ChangedAt = DateTime.UtcNow
        };
        _context.ProjectOwnerHistories.Add(ownerHistory);

        await _context.SaveChangesAsync();

        return await GetProjectById(id);
    }

    public async Task<ProjectDto?> UpdateProjectStatus(int id, string status, string? changedByEmail = null)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return null;

        var oldStatus = project.Status;

        project.Status = status;
        project.UpdatedAt = DateTime.UtcNow;

        // Registrar data de conclusão quando status muda para "Completed"
        if (status == "Completed" && oldStatus != "Completed")
        {
            project.CompletedAt = DateTime.UtcNow;
        }

        _context.Projects.Update(project);

        // Registrar histórico de mudança de status
        var statusHistory = new ProjectStatusHistory
        {
            ProjectId = id,
            FromStatus = oldStatus,
            ToStatus = status,
            ChangedBy = changedByEmail ?? project.Manager,
            ChangedAt = DateTime.UtcNow
        };
        _context.ProjectStatusHistories.Add(statusHistory);

        await _context.SaveChangesAsync();

        return await GetProjectById(id);
    }

    public async Task<bool> DeleteProject(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return false;

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddProjectMember(int projectId, int userId, string role = "Member")
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var existingMember = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

        if (existingMember != null) return false;

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role
        };

        _context.ProjectMembers.Add(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveProjectMember(int projectMemberId)
    {
        var member = await _context.ProjectMembers.FindAsync(projectMemberId);
        if (member == null) return false;

        _context.ProjectMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ProjectMemberDto>> GetProjectMembers(int projectId)
    {
        var members = await _context.ProjectMembers
            .Where(pm => pm.ProjectId == projectId)
            .Include(pm => pm.User)
            .ToListAsync();

        return members.Select(m => new ProjectMemberDto
        {
            Id = m.Id,
            ProjectId = m.ProjectId,
            UserId = m.UserId,
            UserName = m.User?.FullName,
            UserEmail = m.User?.Email,
            Role = m.Role,
            JoinedAt = m.JoinedAt,
            IsActive = m.IsActive
        }).ToList();
    }

    private ProjectDto MapToDto(Project project)
    {
        var managerName = "Desconhecido";
        if (!string.IsNullOrEmpty(project.Manager))
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == project.Manager);
            if (user != null)
            {
                managerName = user.FullName;
            }
        }

        var ownerName = project.Owner?.FullName ?? (project.OwnerId.HasValue ? "Desconhecido" : null);
        var setorName = project.Setor?.Name;

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status,
            Priority = project.Priority,
            Manager = project.Manager,
            ManagerName = managerName,
            OwnerId = project.OwnerId,
            OwnerName = ownerName,
            SetorId = project.SetorId,
            SetorName = setorName,
            FreshDeskId = project.FreshDeskId,
            CommentCount = project.Comments.Count,
            CompletedAt = project.CompletedAt,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            Members = project.ProjectMembers.Select(m => new ProjectMemberDto
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                UserId = m.UserId,
                UserName = m.User?.FullName,
                UserEmail = m.User?.Email,
                Role = m.Role,
                JoinedAt = m.JoinedAt,
                IsActive = m.IsActive
            }).ToList(),
            Tasks = project.Tasks.Select(t => new ProjectTaskDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate,
                AssignedTo = t.AssignedTo,
                EstimatedHours = t.EstimatedHours,
                ActualHours = t.ActualHours,
                Progress = t.Progress,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList()
        };
    }
}
