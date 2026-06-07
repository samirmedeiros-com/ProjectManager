namespace ProjectManagerWebAPI.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public string? Department { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<SetorDto> Setores { get; set; } = new();
}

public class UpdateUserRequest
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public bool IsActive { get; set; }
    public string? Department { get; set; }
    public List<int> SetorIds { get; set; } = new();
}
