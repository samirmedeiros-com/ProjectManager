using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;
using ProjectManagerWebAPI.DTOs;

namespace ProjectManagerWebAPI.Services;

public interface IOraConsoleAuthService
{
    Task<OraConsoleLoginResponse> LoginAsync(OraConsoleLoginRequest request);
    void Logout(string sessionId);
}

public class OraConsoleAuthService : IOraConsoleAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IOraConsoleSessionStore _sessionStore;
    private readonly IOraConsoleAuditLogService _auditLogService;

    public OraConsoleAuthService(IConfiguration configuration, IOraConsoleSessionStore sessionStore, IOraConsoleAuditLogService auditLogService)
    {
        _configuration = configuration;
        _sessionStore = sessionStore;
        _auditLogService = auditLogService;
    }

    public async Task<OraConsoleLoginResponse> LoginAsync(OraConsoleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return new OraConsoleLoginResponse { Success = false, Message = "Utilizador e password são obrigatórios" };

        var connectionString = OraConsoleConnectionHelper.BuildConnectionString(_configuration, request.Username, request.Password);

        try
        {
            await using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT USER FROM DUAL";
            await command.ExecuteScalarAsync();
        }
        catch (OracleException ex)
        {
            return new OraConsoleLoginResponse { Success = false, Message = $"Falha na autenticação Oracle: {ex.Message}" };
        }

        var sessionId = _sessionStore.CreateSession(request.Username, request.Password);
        await _auditLogService.LogLoginAsync(request.Username);

        return new OraConsoleLoginResponse
        {
            Success = true,
            Token = GenerateToken(sessionId, request.Username),
            Username = request.Username
        };
    }

    public void Logout(string sessionId) => _sessionStore.RemoveSession(sessionId);

    private string GenerateToken(string sessionId, string username)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? ""));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            expires: DateTime.UtcNow.AddMinutes(_configuration.GetValue("OraConsole:SessionAbsoluteMinutes", 240)),
            signingCredentials: creds,
            claims: new[]
            {
                new Claim("sub", sessionId),
                new Claim("ora_user", username),
                new Claim("app", "oraconsole")
            }
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
