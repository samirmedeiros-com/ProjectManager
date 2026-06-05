namespace ProjectManagerWebAPI.Models;

public class ProjectOwnerHistory
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string? FromOwner { get; set; }
    public string? ToOwner { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public virtual Project? Project { get; set; }
}
