namespace ProjectManagerWebAPI.DTOs;

public class ForgotPasswordRequest
{
    public required string Email { get; set; }
}

public class ForgotPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}
