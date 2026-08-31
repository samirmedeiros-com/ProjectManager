using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Filters;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models.Kubernetes;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

/// <summary>
/// Gestão de Kubernetes. Tem login próprio (ver <see cref="KubernetesAuthController"/>): o
/// [RequerApp] recusa tokens do Project Manager, do SEUR e do OraConsole, mesmo válidos.
/// </summary>
[ApiController]
[Route("api/kubernetes")]
[Authorize]
[RequerApp(KubernetesAuthService.Aplicacao)]
public class KubernetesController : ControllerBase
{
    private readonly KubernetesGateway _gateway;
    private readonly IKubernetesAuditService _auditoria;
    private readonly IKubernetesNotaService _notas;
    private readonly ILogger<KubernetesController> _logger;

    public KubernetesController(
        KubernetesGateway gateway,
        IKubernetesAuditService auditoria,
        IKubernetesNotaService notas,
        ILogger<KubernetesController> logger)
    {
        _gateway = gateway;
        _auditoria = auditoria;
        _notas = notas;
        _logger = logger;
    }

    [HttpGet("namespaces")]
    public Task<ActionResult<IReadOnlyList<NamespaceInfo>>> Namespaces(CancellationToken ct)
        => ExecutarAsync(() => _gateway.ListarNamespacesAsync(ct), "listar os namespaces");

    [HttpGet("namespaces/{ns}/deployments")]
    public Task<ActionResult<IReadOnlyList<DeploymentInfo>>> Deployments(string ns, CancellationToken ct)
        => ExecutarAsync(
            async () =>
            {
                var lista = await _gateway.ListarDeploymentsAsync(ns, ct);

                // Os títulos vêm da base de dados numa consulta só, e não um pedido por linha:
                // o namespace gateway tem 47 deployments.
                var titulos = await _notas.TitulosAsync(ns);

                return (IReadOnlyList<DeploymentInfo>)lista
                    .Select(d => titulos.TryGetValue(d.Nome, out var titulo) ? d with { Titulo = titulo } : d)
                    .ToList();
            },
            $"listar os deployments de '{ns}'");

    // ── Informação escrita pela equipa ───────────────────────────────────

    [HttpGet("namespaces/{ns}/deployments/{nome}/nota")]
    public async Task<ActionResult<NotaDeploymentDto>> Nota(string ns, string nome)
    {
        try
        {
            // Sem nota devolve-se o objeto vazio e não 404: o popup abre para escrever a primeira.
            return Ok(await _notas.ObterAsync(ns, nome) ?? new NotaDeploymentDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao ler a informação de {Namespace}/{Deployment}.", ns, nome);
            return StatusCode(500, "Não foi possível ler a informação deste deployment.");
        }
    }

    /// <summary>
    /// Grava a informação. Exige o mesmo perfil dos comandos: quem só consulta não escreve.
    /// </summary>
    [HttpPut("namespaces/{ns}/deployments/{nome}/nota")]
    [RequerPapel(KubernetesAuthService.PapelAdmin, KubernetesAuthService.PapelOperador)]
    public async Task<ActionResult<NotaDeploymentDto>> GravarNota(
        string ns, string nome, [FromBody] GravarNotaDto dados)
    {
        if (dados.Titulo is { Length: > KubernetesNotaService.TituloMaximo })
            return BadRequest($"O título não pode ter mais de {KubernetesNotaService.TituloMaximo} caracteres.");

        try
        {
            var email = User.FindFirst("email")?.Value ?? "";
            var quem = User.FindFirst("name")?.Value;

            var (nova, anterior) = await _notas.GravarAsync(ns, nome, dados, email, quem);

            // Só se regista se algo mudou de facto — gravar sem alterar nada não é um evento.
            if (anterior?.Titulo != nova.Titulo || anterior?.Memo != nova.Memo)
            {
                await _auditoria.RegistarAsync(
                    userId: int.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null,
                    email: email,
                    nome: quem,
                    acao: KubernetesAuditService.AcaoNota,
                    ns: ns,
                    deployment: nome,
                    detalhe: anterior is null ? "Informação criada" : "Informação alterada",
                    ip: Ip(),
                    valorAnterior: Texto(anterior?.Titulo, anterior?.Memo),
                    valorNovo: Texto(nova.Titulo, nova.Memo));
            }

            return Ok(nova);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao gravar a informação de {Namespace}/{Deployment}.", ns, nome);
            return StatusCode(500, "Não foi possível gravar a informação deste deployment.");
        }
    }

    /// <summary>Título e memo num só texto, que é como o registo os compara e os mostra.</summary>
    private static string Texto(string? titulo, string? memo)
        => $"{titulo ?? ""}\n{memo ?? ""}".Trim();

    [HttpGet("namespaces/{ns}/deployments/{nome}/pods")]
    public Task<ActionResult<IReadOnlyList<PodInfo>>> Pods(string ns, string nome, CancellationToken ct)
        => ExecutarAsync(() => _gateway.ListarPodsAsync(ns, nome, ct), $"listar os pods de '{nome}'");

    /// <summary>
    /// Consola de um pod. O portal chama isto repetidamente com o <paramref name="desde"/> da
    /// resposta anterior, para receber só as linhas novas em vez do log inteiro de cada vez.
    /// </summary>
    [HttpGet("namespaces/{ns}/pods/{pod}/log")]
    public Task<ActionResult<ResultadoLog>> Log(
        string ns,
        string pod,
        CancellationToken ct,
        [FromQuery] string? contentor = null,
        [FromQuery] int linhas = 500,
        [FromQuery] string? desde = null)
        => ExecutarAsync(
            () => _gateway.LerLogAsync(ns, pod, contentor, linhas, desde, ct),
            $"ler o log de '{pod}'");

    // ── Comandos ─────────────────────────────────────────────────────────
    // São POST e não GET de propósito: mudam o estado do cluster e não podem ser
    // repetidos por um refresh do browser nem apanhados por um pre-fetch.
    // O papel Leitor entra na aplicação e vê tudo, mas não executa nenhum destes.

    [HttpPost("namespaces/{ns}/deployments/{nome}/parar")]
    [RequerPapel(KubernetesAuthService.PapelAdmin, KubernetesAuthService.PapelOperador)]
    public Task<ActionResult<ResultadoComando>> Parar(string ns, string nome, CancellationToken ct)
        => ComandoAsync(ns, nome, () => _gateway.PararAsync(ns, nome, ct), "parar", $"'{nome}' foi parado.");

    [HttpPost("namespaces/{ns}/deployments/{nome}/arrancar")]
    [RequerPapel(KubernetesAuthService.PapelAdmin, KubernetesAuthService.PapelOperador)]
    public Task<ActionResult<ResultadoComando>> Arrancar(string ns, string nome, CancellationToken ct)
        => ComandoAsync(ns, nome, () => _gateway.ArrancarAsync(ns, nome, ct), "arrancar", $"'{nome}' foi arrancado.");

    [HttpPost("namespaces/{ns}/deployments/{nome}/reiniciar")]
    [RequerPapel(KubernetesAuthService.PapelAdmin, KubernetesAuthService.PapelOperador)]
    public Task<ActionResult<ResultadoComando>> Reiniciar(string ns, string nome, CancellationToken ct)
        => ComandoAsync(ns, nome, () => _gateway.ReiniciarAsync(ns, nome, ct), "reiniciar", $"'{nome}' está a reiniciar.");

    private async Task<ActionResult<ResultadoComando>> ComandoAsync(
        string ns, string nome, Func<Task<DeploymentInfo>> operacao, string verbo, string mensagem)
    {
        _logger.LogInformation(
            "Kubernetes: utilizador {UserId} pediu {Verbo} sobre {Namespace}/{Deployment}.",
            User.FindFirst("sub")?.Value, verbo, ns, nome);

        var resposta = await ExecutarAsync(
            async () => new ResultadoComando(mensagem, await operacao()),
            $"{verbo} '{nome}'");

        // Regista-se depois de saber o desfecho, e regista-se **também a falha**: uma tentativa
        // recusada é tão relevante para quem lê o histórico como uma que passou.
        var correu = resposta.Result is null or OkObjectResult;

        await _auditoria.RegistarAsync(
            userId: int.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null,
            email: User.FindFirst("email")?.Value ?? "",
            nome: User.FindFirst("name")?.Value,
            acao: verbo,
            ns: ns,
            deployment: nome,
            sucesso: correu,
            detalhe: correu ? mensagem : Razao(resposta),
            ip: Ip());

        return resposta;
    }

    /// <summary>Texto do erro devolvido, para ficar no registo tal como o utilizador o viu.</summary>
    private static string? Razao(ActionResult<ResultadoComando> resposta)
        => resposta.Result is ObjectResult { Value: string texto } ? texto : "Falhou";

    /// <summary>
    /// IP de quem faz o pedido. Atrás do ingress o endereço direto é o do proxy, por isso o
    /// X-Forwarded-For tem precedência quando existe.
    /// </summary>
    private string? Ip()
    {
        var encaminhado = Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(encaminhado))
            return encaminhado.Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    // ── Registo de ações ─────────────────────────────────────────────────

    /// <summary>
    /// Histórico das ações. Sem `deployment` é a vista global, reservada ao Admin; com
    /// `deployment` é o histórico daquele serviço, que qualquer utilizador da aplicação vê —
    /// saber quem parou o que é justamente o que se quer transparente.
    /// </summary>
    [HttpGet("auditoria")]
    public async Task<ActionResult<PaginaAuditoria>> Auditoria(
        [FromQuery] string? ns = null,
        [FromQuery] string? deployment = null,
        [FromQuery] string? acao = null,
        [FromQuery] string? utilizador = null,
        [FromQuery] int pagina = 0,
        [FromQuery] int tamanho = 25)
    {
        var global = string.IsNullOrWhiteSpace(deployment);

        if (global && User.FindFirst("role")?.Value != KubernetesAuthService.PapelAdmin)
            return StatusCode(403, "O registo global está reservado aos administradores.");

        try
        {
            return Ok(await _auditoria.ConsultarAsync(new FiltroAuditoria
            {
                Namespace = ns,
                Deployment = deployment,
                Acao = acao,
                Utilizador = utilizador,
                Pagina = pagina,
                Tamanho = tamanho
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar o registo de ações.");
            return StatusCode(500, "Não foi possível ler o registo de ações.");
        }
    }

    /// <summary>
    /// Converte as falhas do cluster em respostas legíveis: 502 com a razão devolvida pelo
    /// Kubernetes, nunca uma excepção crua.
    /// </summary>
    private async Task<ActionResult<T>> ExecutarAsync<T>(Func<Task<T>> operacao, string descricao)
    {
        try
        {
            return Ok(await operacao());
        }
        catch (NamespaceNaoPermitidoException ex)
        {
            // 404 e não 403: para quem usa o portal, um namespace fora da lista simplesmente
            // não existe — dizer "não tem permissão" revelaria que existe.
            return NotFound(ex.Message);
        }
        catch (KubernetesException ex)
        {
            _logger.LogError(ex, "Falha ao {Descricao}.", descricao);
            return StatusCode(502, ex.Message);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Pedido cancelado pelo cliente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao {Descricao}.", descricao);
            return StatusCode(500, $"Erro inesperado ao {descricao}.");
        }
    }
}
