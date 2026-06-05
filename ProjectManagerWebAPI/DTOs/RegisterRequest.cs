namespace ProjectManagerWebAPI.DTOs;

public class RegisterRequest
{
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required string Role { get; set; } // "Gestor" ou "Utilizador"
    public string? Department { get; set; }
    public List<int>? SetorIds { get; set; } // IDs dos setores a atribuir
}

public class RegisterResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserDto? User { get; set; }
}
