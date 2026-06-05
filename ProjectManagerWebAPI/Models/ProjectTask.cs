namespace ProjectManagerWebAPI.Models;

public class ProjectTask
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string Status { get; set; } = "Pendente";
    public required string Priority { get; set; } = "Média";
    public DateTime DueDate { get; set; }
    public string? AssignedTo { get; set; }
    public int? EstimatedHours { get; set; }
    public int? ActualHours { get; set; }
    public decimal? Progress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Project? Project { get; set; }
}
