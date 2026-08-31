using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services;

public interface IKubernetesAuthService
{
    Task<K8sLoginResponse> LoginAsync(K8sLoginRequest request, string? ip);
    Task<List<K8sUserDetailDto>> GetAllUsersAsync();
    Task<CreateK8sUserResponseDto> CreateUserAsync(CreateK8sUserDto dto);
    Task<bool> DeactivateUserAsync(int id);
    Task<bool> RemoverUserAsync(int id);
    Task<ResetPasswordResponseDto?> ResetPasswordAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}

/// <summary>
/// Autenticação da Gestão Kubernetes. Credenciais próprias, numa tabela própria: um utilizador
/// do Project Manager, do SEUR ou do OraConsole não entra aqui, e o token emitido também não
/// serve nas outras aplicações — o claim "app" é verificado pelo <see cref="Filters.RequerAppAttribute"/>.
/// </summary>
public class KubernetesAuthService : IKubernetesAuthService
{
    /// <summary>Valor do claim "app" nos tokens desta aplicação.</summary>
    public const string Aplicacao = "kubernetes";

    public const string PapelAdmin = "Admin";
    public const string PapelOperador = "Operador";
    public const string PapelLeitor = "Leitor";

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IKubernetesAuditService _auditoria;
    private readonly KubernetesOptions _opcoes;
    private readonly ILogger<KubernetesAuthService> _logger;

    public KubernetesAuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IEmailService emailService,
        IKubernetesAuditService auditoria,
        IOptions<KubernetesOptions> opcoes,
        ILogger<KubernetesAuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _auditoria = auditoria;
        _opcoes = opcoes.Value;
        _logger = logger;
    }

    public async Task<K8sLoginResponse> LoginAsync(K8sLoginRequest request, string? ip)
    {
        var user = await _context.KubernetesUsers.FirstOrDefaultAsync(u => u.Email == request.Email);

        // A mesma mensagem para email desconhecido e password errada: dizer qual dos dois
        // falhou transformaria o ecrã de login num verificador de contas. No registo, porém,
        // guarda-se a razão real — é lá que se investiga uma tentativa de entrada.
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            await _auditoria.RegistarAsync(
                user?.Id, request.Email, user?.FullName, KubernetesAuditService.AcaoLoginFalhado,
                sucesso: false,
                detalhe: user is null ? "Email desconhecido" : "Password inválida",
                ip: ip);

            return new K8sLoginResponse { Success = false, Message = "Email ou password inválidos" };
        }

        if (!user.IsActive)
        {
            await _auditoria.RegistarAsync(
                user.Id, user.Email, user.FullName, KubernetesAuditService.AcaoLoginFalhado,
                sucesso: false, detalhe: "Conta inativa", ip: ip);

            return new K8sLoginResponse { Success = false, Message = "Conta de utilizador inativa" };
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditoria.RegistarAsync(
            user.Id, user.Email, user.FullName, KubernetesAuditService.AcaoLogin, ip: ip);

        return new K8sLoginResponse
        {
            Success = true,
            Token = GenerateToken(user),
            User = new K8sUserDto { Id = user.Id, Email = user.Email, FullName = user.FullName, Role = user.Role }
        };
    }

    public async Task<List<K8sUserDetailDto>> GetAllUsersAsync()
    {
        // Sem Where sobre o bool IsActive: o provider Oracle rebenta a gerar o literal
        // booleano do SQL. Lê-se tudo (a tabela é pequena) e filtra-se em memória se preciso.
        return await _context.KubernetesUsers
            .OrderBy(u => u.FullName)
            .Select(u => new K8sUserDetailDto
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

    public async Task<CreateK8sUserResponseDto> CreateUserAsync(CreateK8sUserDto dto)
    {
        var existente = await _context.KubernetesUsers.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existente is not null)
            throw new InvalidOperationException("Email já registado");

        var password = GenerateRandomPassword();

        var user = new KubernetesUser
        {
            Email = dto.Email,
            FullName = dto.FullName,
            PasswordHash = HashPassword(password),
            Role = NormalizarPapel(dto.Role),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.KubernetesUsers.Add(user);
        await _context.SaveChangesAsync();

        var emailEnviado = await EnviarPasswordAsync(
            user.Email,
            "Conta criada — Gestão Kubernetes",
            "A sua conta foi criada na <b>Gestão Kubernetes</b>.",
            password);

        return new CreateK8sUserResponseDto
        {
            User = new K8sUserDetailDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = true,
                CreatedAt = user.CreatedAt
            },
            TempPassword = password,
            EmailSent = emailEnviado
        };
    }

    public async Task<bool> DeactivateUserAsync(int id)
    {
        var user = await _context.KubernetesUsers.FindAsync(id);
        if (user is null) return false;

        user.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Apaga a conta de vez. Serve para contas criadas por engano — desativar deixaria a lista
    /// a encher-se de entradas mortas.
    ///
    /// O histórico de ações **não** é tocado: cada linha guarda o nome e o email de quem agiu,
    /// por isso continua legível depois de a conta desaparecer.
    /// </summary>
    public async Task<bool> RemoverUserAsync(int id)
    {
        var user = await _context.KubernetesUsers.FindAsync(id);
        if (user is null) return false;

        _context.KubernetesUsers.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ResetPasswordResponseDto?> ResetPasswordAsync(int userId)
    {
        var user = await _context.KubernetesUsers.FindAsync(userId);
        if (user is null) return null;

        var password = GenerateRandomPassword();
        user.PasswordHash = HashPassword(password);
        await _context.SaveChangesAsync();

        var emailEnviado = await EnviarPasswordAsync(
            user.Email,
            "Password reposta — Gestão Kubernetes",
            "A sua password foi reposta na <b>Gestão Kubernetes</b>.",
            password);

        return new ResetPasswordResponseDto { TempPassword = password, EmailSent = emailEnviado };
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.KubernetesUsers.FindAsync(userId);
        if (user is null || !VerifyPassword(currentPassword, user.PasswordHash)) return false;

        user.PasswordHash = HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Token ────────────────────────────────────────────────────────────

    public string GenerateToken(KubernetesUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? ""));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(jwtSettings["ExpiryMinutes"])),
            signingCredentials: creds,
            claims:
            [
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("name", user.FullName),
                new Claim("role", user.Role ?? PapelLeitor),
                // Todas as aplicações do portal assinam com a mesma chave, por isso o token
                // sozinho não distingue origens: é este claim, verificado pelo RequerApp,
                // que impede um token do Project Manager ou do SEUR de abrir esta API.
                new Claim("app", Aplicacao)
            ]);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Auxiliares ───────────────────────────────────────────────────────

    private async Task<bool> EnviarPasswordAsync(string email, string assunto, string introHtml, string password)
    {
        var url = _opcoes.UrlLogin;

        try
        {
            await _emailService.SendEmailAsync(
                email,
                assunto,
                $"{assunto}\n\nEndereço de acesso: {url}\nEmail: {email}\n" +
                $"Password temporária: {password}\n\nAltere a password após o primeiro acesso.",
                $"<p>{introHtml}</p>" +
                $"<p><b>Endereço de acesso:</b> <a href=\"{url}\">{url}</a><br>" +
                $"<b>Email:</b> {email}<br>" +
                $"<b>Password temporária:</b> <code style='font-size:16px'>{password}</code></p>" +
                "<p>Altere a password após o primeiro acesso.</p>");
            return true;
        }
        catch (Exception ex)
        {
            // Uma falha de SMTP não pode desfazer o utilizador já gravado: a password
            // temporária volta na resposta para o administrador a entregar à mão.
            _logger.LogWarning(ex, "Não foi possível enviar a password para {Email}.", email);
            return false;
        }
    }

    private static string NormalizarPapel(string? papel) => papel switch
    {
        PapelAdmin => PapelAdmin,
        PapelOperador => PapelOperador,
        _ => PapelLeitor
    };

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#";
        var bytes = RandomNumberGenerator.GetBytes(12);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    private static bool VerifyPassword(string password, string hash)
        => HashPassword(password) == hash;
}
