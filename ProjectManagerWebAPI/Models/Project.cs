namespace ProjectManagerWebAPI.Models;

public class Project
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required string Status { get; set; } = "Planejamento";
    public int Priority { get; set; } = 1;
    public string? Manager { get; set; }
    public string? FreshDeskId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? OwnerId { get; set; }
    public int? SetorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual User? Owner { get; set; }
    public virtual Setor? Setor { get; set; }
    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public virtual ICollection<ProjectStatusHistory> StatusHistory { get; set; } = new List<ProjectStatusHistory>();
    public virtual ICollection<ProjectManagerHistory> ManagerHistory { get; set; } = new List<ProjectManagerHistory>();
    public virtual ICollection<ProjectOwnerHistory> OwnerHistory { get; set; } = new List<ProjectOwnerHistory>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
