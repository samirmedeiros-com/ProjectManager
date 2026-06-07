using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services;

public interface IAuthService
{
    LoginResponse Login(LoginRequest request);
    bool Register(string email, string fullName, string password, string? department = null);
    RegisterResponse Register(RegisterRequest request);
    User? GetUserByEmail(string email);
    Task<List<UserDto>> GetAllUsers();
    string GenerateToken(User user);
    RegisterResponse ChangePassword(string email, string currentPassword, string newPassword);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(string email);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IPasswordService passwordService,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _passwordService = passwordService;
        _emailService = emailService;
        _logger = logger;
    }

    public LoginResponse Login(LoginRequest request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            return new LoginResponse { Success = false, Message = "Email ou senha inválidos" };
        }

        if (!user.IsActive)
        {
            return new LoginResponse { Success = false, Message = "Conta de usuário inativa" };
        }

        user.LastLoginAt = DateTime.UtcNow;
        _context.SaveChanges();

        var token = GenerateToken(user);

        return new LoginResponse
        {
            Success = true,
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Department = user.Department,
                Role = user.Role
            }
        };
    }

    public bool Register(string email, string fullName, string password, string? department = null)
    {
        if (_context.Users.Any(u => u.Email == email))
        {
            return false;
        }

        var user = new User
        {
            Email = email,
            FullName = fullName,
            PasswordHash = HashPassword(password),
            Department = department
        };

        _context.Users.Add(user);
        _context.SaveChanges();
        return true;
    }

    public RegisterResponse Register(RegisterRequest request)
    {
        // Usar FirstOrDefault ao invés de Any para evitar CASE WHEN com TRUE/FALSE no Oracle
        if (_context.Users.FirstOrDefault(u => u.Email == request.Email) != null)
        {
            return new RegisterResponse
            {
                Success = false,
                Message = "Email já existe"
            };
        }

        // Gerar password aleatória
        var generatedPassword = _passwordService.GenerateRandomPassword(12);

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = HashPassword(generatedPassword),
            Department = request.Department,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        // Atribuir setores se fornecidos
        if (request.SetorIds?.Any() == true)
        {
            foreach (var setorId in request.SetorIds)
            {
                var userSetor = new UserSetor
                {
                    UserId = user.Id,
                    SetorId = setorId,
                    AssignedAt = DateTime.UtcNow
                };
                _context.UserSetores.Add(userSetor);
            }
            _context.SaveChanges();
        }

        // Enviar email com a password gerada
        var emailRequest = new Models.EmailRequest
        {
            To = request.Email,
            Subject = "Bem-vindo ao Project Manager - Credenciais de Acesso",
            HtmlBody = $@"
                <h2>Bem-vindo ao DPD - Project Manager!</h2>
                <p>Olá <strong>{request.FullName}</strong>,</p>
                <p>Sua conta foi criada com sucesso. Use as credenciais abaixo para fazer login:</p>

                <div style='background-color: #f5f5f5; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                    <p><strong>Email:</strong> {request.Email}</p>
                    <p><strong>Password Temporária:</strong> <code style='background-color: #e0e0e0; padding: 5px 10px; border-radius: 3px;'>{generatedPassword}</code></p>
                </div>

                <p><strong>Importante:</strong> Por sua segurança, recomendamos que altere esta password no seu primeiro acesso.</p>

                <p>Para fazer login, visite: <a href='http://10.2.6.81/login'>DPD Project Manager</a></p>

                <hr style='border: none; border-top: 1px solid #ccc; margin: 20px 0;'>
                <p style='color: #666; font-size: 12px;'>
                    Este é um email automático. Por favor, não responda directamente. <br>
                    Se tiver dúvidas, contacte o administrador do sistema.
                </p>
            "
        };

        var emailResponse = _emailService.SendEmailAsync(emailRequest).GetAwaiter().GetResult();

        if (!emailResponse.Success)
        {
            _logger.LogWarning($"Falha ao enviar email de boas-vindas para {request.Email}: {emailResponse.Message}");
        }

        return new RegisterResponse
        {
            Success = true,
            Message = "Utilizador registrado com sucesso. Um email com as credenciais foi enviado.",
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Department = user.Department,
                Role = user.Role
            }
        };
    }

    public User? GetUserByEmail(string email)
    {
        return _context.Users.FirstOrDefault(u => u.Email == email);
    }

    public async Task<List<UserDto>> GetAllUsers()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            Department = u.Department,
            Role = u.Role
        }).ToList();
    }

    public string GenerateToken(User user)
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
                new System.Security.Claims.Claim("sub", user.Id.ToString()),
                new System.Security.Claims.Claim("email", user.Email),
                new System.Security.Claims.Claim("name", user.FullName),
                new System.Security.Claims.Claim("role", user.Role ?? "Utilizador")
            }
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    private bool VerifyPassword(string password, string hash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }

    public RegisterResponse ChangePassword(string email, string currentPassword, string newPassword)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            return new RegisterResponse
            {
                Success = false,
                Message = "Utilizador não encontrado"
            };
        }

        if (!VerifyPassword(currentPassword, user.PasswordHash))
        {
            return new RegisterResponse
            {
                Success = false,
                Message = "Password atual incorreta"
            };
        }

        user.PasswordHash = HashPassword(newPassword);
        _context.SaveChanges();

        return new RegisterResponse
        {
            Success = true,
            Message = "Password alterada com sucesso"
        };
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return new ForgotPasswordResponse
            {
                Success = false,
                Message = "Email não encontrado"
            };
        }

        // Gerar nova password
        var newPassword = _passwordService.GenerateRandomPassword(12);
        user.PasswordHash = HashPassword(newPassword);
        _context.SaveChanges();

        // Enviar email com a nova password
        var emailRequest = new Models.EmailRequest
        {
            To = email,
            Subject = "Recuperação de Password - Project Manager",
            HtmlBody = $@"
                <h2>Recuperação de Password</h2>
                <p>Olá <strong>{user.FullName}</strong>,</p>
                <p>Recebemos um pedido para recuperar a sua password. Aqui está a sua nova password temporária:</p>

                <div style='background-color: #f5f5f5; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                    <p><strong>Email:</strong> {email}</p>
                    <p><strong>Password Temporária:</strong> <code style='background-color: #e0e0e0; padding: 5px 10px; border-radius: 3px;'>{newPassword}</code></p>
                </div>

                <p><strong>Importante:</strong> Por sua segurança, recomendamos que altere esta password no seu primeiro acesso.</p>

                <p>Se você não solicitou esta recuperação, ignore este email.</p>

                <hr style='border: none; border-top: 1px solid #ccc; margin: 20px 0;'>
                <p style='color: #666; font-size: 12px;'>
                    Este é um email automático. Por favor, não responda directamente. <br>
                    Se tiver dúvidas, contacte o administrador do sistema.
                </p>
            "
        };

        var emailResponse = await _emailService.SendEmailAsync(emailRequest);

        if (!emailResponse.Success)
        {
            _logger.LogWarning($"Falha ao enviar email de recuperação para {email}: {emailResponse.Message}");
        }

        return new ForgotPasswordResponse
        {
            Success = true,
            Message = "Uma nova password foi enviada para o seu email"
        };
    }
}
