namespace ProjectManagerWebAPI.Models;

public class Comment
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public required string Content { get; set; }
    public string? Author { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Project? Project { get; set; }
}
