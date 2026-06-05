namespace ProjectManagerWebAPI.DTOs;

public class ProjectTaskDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public DateTime DueDate { get; set; }
    public string? AssignedTo { get; set; }
    public int? EstimatedHours { get; set; }
    public int? ActualHours { get; set; }
    public decimal? Progress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTaskRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required DateTime DueDate { get; set; }
    public string? AssignedTo { get; set; }
    public int? EstimatedHours { get; set; }
    public string? Priority { get; set; } = "Medium";
}

public class UpdateTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? AssignedTo { get; set; }
    public int? ActualHours { get; set; }
    public decimal? Progress { get; set; }
}
