using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ProjectManagerWebAPI;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Services;
using ProjectManagerWebAPI.Services.Tv;

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

// Mural de TV (sem login, acesso por chave no URL)
builder.Services.Configure<TvDashboardOptions>(builder.Configuration.GetSection(TvDashboardOptions.Seccao));
builder.Services.AddScoped<ITvDashboardService, TvDashboardService>();

// Cada fonte contribui um bloco do mural, de forma independente das outras.
// Acrescentar uma origem de dados nova é escrever um ITvFonte e registá-lo aqui.
// A fonte de projetos (TvProjetosFonte) fica de fora de propósito: o mural não tem
// nenhum card dela e o registo custava ~1s de queries por ciclo. A classe fica no
// código — para a repor basta acrescentar aqui a linha e declarar os cards.
builder.Services.AddScoped<ITvFonte, TvSeurFonte>();
builder.Services.AddScoped<ITvFonte, TvShpnotFonte>();
builder.Services.AddScoped<ITvFonte, TvTteventosFonte>();
builder.Services.AddScoped<ITvFonte, TvTracingFonte>();
builder.Services.AddScoped<ITvFonte, TvAs400Fonte>();

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

// Gestão de Kubernetes (restrito ao setor IT) — sub-app dentro do monólito, como o OpenSearch
builder.Services.AddScoped<IKubernetesAuditService, KubernetesAuditService>();
builder.Services.AddScoped<IKubernetesNotaService, KubernetesNotaService>();
builder.Services.AddScoped<IKubernetesAuthService, KubernetesAuthService>();
builder.Services.Configure<KubernetesOptions>(builder.Configuration.GetSection(KubernetesOptions.Seccao));

builder.Services.AddHttpClient<KubernetesGateway>((sp, http) =>
{
    var opcoes = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KubernetesOptions>>().Value;

    if (!string.IsNullOrWhiteSpace(opcoes.BaseUrl))
        http.BaseAddress = new Uri(opcoes.BaseUrl.TrimEnd('/') + "/");

    http.Timeout = TimeSpan.FromSeconds(opcoes.TimeoutSegundos);

    if (!string.IsNullOrWhiteSpace(opcoes.Token))
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opcoes.Token);
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var opcoes = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KubernetesOptions>>().Value;
    var handler = new HttpClientHandler();

    // O servidor de API do OKE apresenta um certificado assinado pela CA do próprio cluster,
    // que não está no armazém de confiança do servidor da aplicação.
    if (opcoes.IgnorarCertificado)
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

    return handler;
});

// Gestão de Dados — contas e subcontas (WSDPD) e envio ao portal de clientes.
// Usa as credenciais da Gestão SEUR, como a Consulta OpenSearch: o [RequerApp("seur")] no
// controlador é que separa as sessões, já que todas as apps assinam o JWT com a mesma chave.
builder.Services.Configure<ContasOptions>(builder.Configuration.GetSection(ContasOptions.Seccao));
builder.Services.AddScoped<IContasRepository, ContasRepository>();
builder.Services.AddScoped<IContasPortalSender, ContasPortalSender>();
builder.Services.AddScoped<IContasAuditService, ContasAuditService>();

// Trace Push — módulo da Gestão de Dados sobre CHRONO_WEB.CW_TRACEPUSH (mesma ligação das contas).
builder.Services.AddScoped<ITracePushRepository, TracePushRepository>();
builder.Services.AddScoped<ITracePushAuditService, TracePushAuditService>();

// Kafka — módulo da Gestão de Dados sobre a REST API do Kafka Connect.
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.Seccao));
builder.Services.AddScoped<IKafkaConnectGateway, KafkaConnectGateway>();
builder.Services.AddHttpClient("KafkaConnect");

// Cliente nomeado (e não tipado): os endereços mudam com o ambiente escolhido em cada envio,
// por isso não há BaseAddress fixo — cada pedido leva o URL completo.
builder.Services.AddHttpClient("ContasPortal", (sp, http) =>
{
    var opcoes = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ContasOptions>>().Value;
    http.Timeout = TimeSpan.FromSeconds(opcoes.TimeoutSegundos);
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
        SeedKubernetesAdmin.CreateKubernetesAdminUser(db);
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

try
{
    ContasLogSchemaInitializer.EnsureLogTables(builder.Configuration);
    TracePushLogSchemaInitializer.EnsureLogTables(builder.Configuration);
}
catch (Exception ex)
{
    Console.WriteLine($"Erro ao criar tabelas de log de envios: {ex.Message}");
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
