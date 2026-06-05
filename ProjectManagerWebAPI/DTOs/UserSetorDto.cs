namespace ProjectManagerWebAPI.DTOs;

public class UserSetorDto
{
    public int Id { get; set; }
    public int SetorId { get; set; }
    public required string SetorName { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class AssignSetoresRequest
{
    public required List<int> SetorIds { get; set; }
}
