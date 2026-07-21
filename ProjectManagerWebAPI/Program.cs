using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ProjectManagerWebAPI;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "ProjectManagerWebAPI_SuperSecretKey_2026");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = "role"
    };
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseOracle(connectionString);
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISeurAuthService, SeurAuthService>();
builder.Services.AddScoped<ISeurGuiaService, SeurGuiaService>();
builder.Services.AddScoped<ISeurTabelasService, SeurTabelasService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ISetorService, SetorService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddSingleton<IOraConsoleSessionStore, OraConsoleSessionStore>();
builder.Services.AddScoped<IOraConsoleAuthService, OraConsoleAuthService>();
builder.Services.AddScoped<IOraConsoleSchemaService, OraConsoleSchemaService>();
builder.Services.AddScoped<IOraConsoleQueryService, OraConsoleQueryService>();
builder.Services.AddScoped<IOraConsoleAuditLogService, OraConsoleAuditLogService>();
builder.Services.AddScoped<ISetorAccessService, SetorAccessService>();

// Portal de consulta ao OpenSearch (restrito ao setor IT)
builder.Services.Configure<OpenSearchOptions>(builder.Configuration.GetSection(OpenSearchOptions.Seccao));

builder.Services.AddHttpClient<OpenSearchGateway>((sp, http) =>
{
    var opcoes = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenSearchOptions>>().Value;

    if (!string.IsNullOrWhiteSpace(opcoes.BaseUrl))
        http.BaseAddress = new Uri(opcoes.BaseUrl.TrimEnd('/') + "/");

    http.Timeout = TimeSpan.FromSeconds(opcoes.TimeoutSegundos);

    if (!string.IsNullOrWhiteSpace(opcoes.Utilizador))
    {
        var credenciais = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{opcoes.Utilizador}:{opcoes.Password}"));
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credenciais);
    }
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var opcoes = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenSearchOptions>>().Value;
    var handler = new HttpClientHandler();

    // O cluster gerido da OCI usa certificado auto-assinado e é acedido por IP privado.
    if (opcoes.IgnorarCertificado)
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

    return handler;
});

// Configurar SmtpSettings
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

var app = builder.Build();

// Executar migrações apenas uma vez
// Criar apenas o usuário admin padrão
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();

        SeedAdminAndOwner.CreateAdminAndOwnerUsers(db);
        SeedSeurAdmin.CreateSeurAdminUser(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao aplicar migrações: {ex.Message}");
    }
}

try
{
    OraConsoleLogSchemaInitializer.EnsureLogTable(builder.Configuration);
}
catch (Exception ex)
{
    Console.WriteLine($"Erro ao criar tabela de log OraConsole: {ex.Message}");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
