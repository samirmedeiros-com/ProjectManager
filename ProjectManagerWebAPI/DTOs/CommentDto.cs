namespace ProjectManagerWebAPI.DTOs;

public class CommentDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCommentRequest
{
    public required string Content { get; set; }
}
