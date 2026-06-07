namespace ProjectManagerWebAPI.DTOs;

public class SetorDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSetorRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateSetorRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
