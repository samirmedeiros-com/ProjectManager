namespace ProjectManagerWebAPI.Models;

public class ProjectManagerHistory
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public required string FromManager { get; set; }
    public required string ToManager { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public virtual Project? Project { get; set; }
}
