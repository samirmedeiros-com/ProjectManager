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
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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
        if (_context.Users.Any(u => u.Email == request.Email))
        {
            return new RegisterResponse
            {
                Success = false,
                Message = "Email já existe"
            };
        }

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = HashPassword(request.Password),
            Department = request.Department,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return new RegisterResponse
        {
            Success = true,
            Message = "Usuário registrado com sucesso",
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
}
