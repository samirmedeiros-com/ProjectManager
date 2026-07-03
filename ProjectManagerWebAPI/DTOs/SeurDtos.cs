namespace ProjectManagerWebAPI.DTOs;

public class SeurLoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class SeurUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class SeurLoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
    public SeurUserDto? User { get; set; }
}

public class SeurUserDetailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class CreateSeurUserDto
{
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public string Role { get; set; } = "Utilizador";
}

public class ChangePasswordDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}

public class CreateUserResponseDto
{
    public SeurUserDetailDto User { get; set; } = null!;
    public string TempPassword { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
}

public class ResetPasswordResponseDto
{
    public string TempPassword { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
}
