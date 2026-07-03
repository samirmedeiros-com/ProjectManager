using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services;

public interface ISeurAuthService
{
    SeurLoginResponse Login(SeurLoginRequest request);
    SeurUser? GetUserByEmail(string email);
    Task<List<SeurUserDetailDto>> GetAllUsersAsync();
    string GenerateToken(SeurUser user);
    Task<CreateUserResponseDto> CreateUserAsync(CreateSeurUserDto dto);
    Task<bool> DeactivateUserAsync(int id);
    Task<ResetPasswordResponseDto?> ResetPasswordAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<(bool Success, string Message)> ForgotPasswordAsync(string email);
}

public class SeurAuthService : ISeurAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public SeurAuthService(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    public SeurLoginResponse Login(SeurLoginRequest request)
    {
        var user = _context.SeurUsers.FirstOrDefault(u => u.Email == request.Email);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            return new SeurLoginResponse { Success = false, Message = "Email ou password inválidos" };

        if (!user.IsActive)
            return new SeurLoginResponse { Success = false, Message = "Conta de utilizador inativa" };

        user.LastLoginAt = DateTime.UtcNow;
        _context.SaveChanges();

        return new SeurLoginResponse
        {
            Success = true,
            Token = GenerateToken(user),
            User = new SeurUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            }
        };
    }

    public SeurUser? GetUserByEmail(string email)
        => _context.SeurUsers.FirstOrDefault(u => u.Email == email);

    public async Task<List<SeurUserDetailDto>> GetAllUsersAsync()
    {
        return await _context.SeurUsers
            .OrderBy(u => u.FullName)
            .Select(u => new SeurUserDetailDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();
    }

    public async Task<CreateUserResponseDto> CreateUserAsync(CreateSeurUserDto dto)
    {
        if (await _context.SeurUsers.FirstOrDefaultAsync(u => u.Email == dto.Email) != null)
            throw new InvalidOperationException("Email já registado");

        var password = GenerateRandomPassword();

        var user = new SeurUser
        {
            Email = dto.Email,
            FullName = dto.FullName,
            PasswordHash = HashPassword(password),
            Role = dto.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.SeurUsers.Add(user);
        await _context.SaveChangesAsync();

        var emailSent = false;
        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Conta criada — Gestão SEUR",
                $"A sua conta foi criada.\n\nEmail: {user.Email}\nPassword temporária: {password}\n\nPor favor altere a password após o primeiro login.",
                $"<p>A sua conta foi criada na <b>Gestão SEUR</b>.</p><p><b>Email:</b> {user.Email}<br><b>Password temporária:</b> <code style='font-size:16px'>{password}</code></p><p>Por favor altere a password após o primeiro login.</p>"
            );
            emailSent = true;
        }
        catch { /* falha de email não bloqueia criação */ }

        return new CreateUserResponseDto
        {
            User = new SeurUserDetailDto { Id = user.Id, Email = user.Email, FullName = user.FullName, Role = user.Role, IsActive = true, CreatedAt = user.CreatedAt },
            TempPassword = password,
            EmailSent = emailSent
        };
    }

    public async Task<bool> DeactivateUserAsync(int id)
    {
        var user = await _context.SeurUsers.FindAsync(id);
        if (user == null) return false;
        user.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ResetPasswordResponseDto?> ResetPasswordAsync(int userId)
    {
        var user = await _context.SeurUsers.FindAsync(userId);
        if (user == null) return null;

        var password = GenerateRandomPassword();
        user.PasswordHash = HashPassword(password);
        await _context.SaveChangesAsync();

        var emailSent = false;
        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Password reposta — Gestão SEUR",
                $"A sua password foi reposta.\n\nEmail: {user.Email}\nNova password temporária: {password}\n\nPor favor altere a password após o login.",
                $"<p>A sua password foi reposta na <b>Gestão SEUR</b>.</p><p><b>Email:</b> {user.Email}<br><b>Nova password temporária:</b> <code style='font-size:16px'>{password}</code></p><p>Por favor altere a password após o login.</p>"
            );
            emailSent = true;
        }
        catch { }

        return new ResetPasswordResponseDto { TempPassword = password, EmailSent = emailSent };
    }

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
    {
        var user = await _context.SeurUsers.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (user == null)
            return (false, "Email não encontrado ou conta inativa");

        var password = GenerateRandomPassword();
        user.PasswordHash = HashPassword(password);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendEmailAsync(
                email,
                "Recuperação de Password — Gestão SEUR",
                $"A sua password foi reposta.\n\nEmail: {email}\nNova password temporária: {password}\n\nPor favor altere a password após o login.",
                $"<p>Recebemos um pedido de recuperação de password para a <b>Gestão SEUR</b>.</p>" +
                $"<p><b>Email:</b> {email}<br><b>Nova password temporária:</b> <code style='font-size:16px;background:#f5f5f5;padding:4px 8px;border-radius:3px'>{password}</code></p>" +
                $"<p>Por favor altere a password após o login.</p>"
            );
        }
        catch { /* falha de email não bloqueia */ }

        return (true, "Uma nova password foi enviada para o seu email");
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.SeurUsers.FindAsync(userId);
        if (user == null || !VerifyPassword(currentPassword, user.PasswordHash)) return false;
        user.PasswordHash = HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#";
        var random = System.Security.Cryptography.RandomNumberGenerator.GetBytes(10);
        return new string(random.Select(b => chars[b % chars.Length]).ToArray());
    }

    public string GenerateToken(SeurUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? ""));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(jwtSettings["ExpiryMinutes"])),
            signingCredentials: creds,
            claims: new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("name", user.FullName),
                new Claim("role", user.Role ?? "Utilizador"),
                new Claim("app", "seur")
            }
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hash)
        => HashPassword(password) == hash;
}
