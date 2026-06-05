namespace ProjectManagerWebAPI.DTOs;

public class ProjectMemberDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }
}

public class AddProjectMemberRequest
{
    public required int UserId { get; set; }
    public string? Role { get; set; } = "Membro";
}
