namespace ProjectManagerWebAPI.DTOs;

public class ProjectDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public int Priority { get; set; }
    public string? Manager { get; set; }
    public string? ManagerName { get; set; }
    public int? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public int? SetorId { get; set; }
    public string? SetorName { get; set; }
    public string? FreshDeskId { get; set; }
    public int CommentCount { get; set; } = 0;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ProjectMemberDto>? Members { get; set; }
    public List<ProjectTaskDto>? Tasks { get; set; }
}

public class CreateProjectRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Priority { get; set; } = 1;
    public required string Manager { get; set; }
    public int? OwnerId { get; set; }
    public int? SetorId { get; set; }
    public string? FreshDeskId { get; set; }
}

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public int? Priority { get; set; }
    public int? OwnerId { get; set; }
    public int? SetorId { get; set; }
    public string? FreshDeskId { get; set; }
}

public class UpdateManagerRequest
{
    public required string Manager { get; set; }
}

public class UpdateStatusRequest
{
    public required string Status { get; set; }
}

public class UpdateOwnerRequest
{
    public int? OwnerId { get; set; }
}
