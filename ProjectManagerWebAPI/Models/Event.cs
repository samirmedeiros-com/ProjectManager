namespace ProjectManagerWebAPI.Models;

public class Event
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty; // Formato: HH:mm
    public string EndTime { get; set; } = string.Empty;   // Formato: HH:mm
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public bool IsApplicableToProject { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
