namespace ProjectManagerWebAPI.Models;

public class ProjectMember
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public required string Role { get; set; } = "Membro";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public virtual Project? Project { get; set; }
    public virtual User? User { get; set; }
}
