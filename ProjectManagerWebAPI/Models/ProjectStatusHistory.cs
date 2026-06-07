namespace ProjectManagerWebAPI.Models;

public class ProjectStatusHistory
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public required string FromStatus { get; set; }
    public required string ToStatus { get; set; }
    public string? Reason { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public virtual Project? Project { get; set; }
}
