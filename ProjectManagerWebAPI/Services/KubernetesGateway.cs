using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProjectManagerWebAPI.Models.Kubernetes;

namespace ProjectManagerWebAPI.Services;

/// <summary>
/// Falha vinda do cluster (ou da ligação a ele). O controlador traduz isto num 502 com
/// mensagem legível, para o portal nunca mostrar uma stack trace ao utilizador.
/// </summary>
public sealed class KubernetesException(string mensagem, Exception? inner = null)
    : Exception(mensagem, inner);

/// <summary>Pedido para um namespace que não está na lista configurada.</summary>
public sealed class NamespaceNaoPermitidoException(string ns)
    : Exception($"O namespace '{ns}' não está disponível neste portal.");

/// <summary>
/// Acesso à API REST do Kubernetes. Sem cliente oficial — só HTTP + JSON, como o
/// <see cref="OpenSearchGateway"/>: o que aqui se faz são cinco chamadas simples e o pacote
/// KubernetesClient traria um modelo de objetos inteiro para as servir.
/// </summary>
public sealed class KubernetesGateway(HttpClient http, IOptions<KubernetesOptions> opcoes, ILogger<KubernetesGateway> logger)
{
    private readonly KubernetesOptions _opcoes = opcoes.Value;

    /// <summary>
    /// Onde ficam as réplicas originais quando o portal para um deployment. Sem isto, arrancar
    /// de novo teria de adivinhar o número — e um deployment de 4 réplicas voltaria com 1.
    /// </summary>
    public const string AnotacaoReplicas = "projectmanager.dpd.pt/replicas-antes-de-parar";

    private const string AnotacaoReinicio = "kubectl.kubernetes.io/restartedAt";

    // ── Namespaces ───────────────────────────────────────────────────────

    public IReadOnlyList<string> NamespacesPermitidos => _opcoes.Namespaces;

    public async Task<IReadOnlyList<NamespaceInfo>> ListarNamespacesAsync(CancellationToken ct)
    {
        var lista = new List<NamespaceInfo>();

        foreach (var ns in _opcoes.Namespaces)
        {
            // Um namespace inacessível (apagado, RBAC em falta) não pode derrubar a página
            // inteira: aparece a zeros e o utilizador vê o erro ao entrar nele.
            try
            {
                var deployments = await ListarDeploymentsAsync(ns, ct);
                lista.Add(new NamespaceInfo(
                    Nome: ns,
                    TotalDeployments: deployments.Count,
                    DeploymentsProntos: deployments.Count(d => d.Estado == "pronto"),
                    DeploymentsParados: deployments.Count(d => d.Estado == "parado")));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Não foi possível resumir o namespace {Namespace}.", ns);
                lista.Add(new NamespaceInfo(ns, 0, 0, 0));
            }
        }

        return lista;
    }

    // ── Deployments ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<DeploymentInfo>> ListarDeploymentsAsync(string ns, CancellationToken ct)
    {
        GarantirNamespace(ns);

        var resposta = await LerAsync($"apis/apps/v1/namespaces/{Uri.EscapeDataString(ns)}/deployments", ct);

        var lista = new List<DeploymentInfo>();
        if (resposta.TryGetProperty("items", out var itens))
        {
            foreach (var item in itens.EnumerateArray())
                lista.Add(LerDeployment(ns, item));
        }

        return lista.OrderBy(d => d.Nome, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<DeploymentInfo> ObterDeploymentAsync(string ns, string nome, CancellationToken ct)
    {
        GarantirNamespace(ns);

        var resposta = await LerAsync(
            $"apis/apps/v1/namespaces/{Uri.EscapeDataString(ns)}/deployments/{Uri.EscapeDataString(nome)}", ct);

        return LerDeployment(ns, resposta);
    }

    // ── Pods de um deployment ────────────────────────────────────────────

    public async Task<IReadOnlyList<PodInfo>> ListarPodsAsync(string ns, string deployment, CancellationToken ct)
    {
        GarantirNamespace(ns);

        var doDeployment = await LerAsync(
            $"apis/apps/v1/namespaces/{Uri.EscapeDataString(ns)}/deployments/{Uri.EscapeDataString(deployment)}", ct);

        // Os pods pedem-se pelo selector do deployment, não pelo nome: o nome do pod inclui
        // o hash do ReplicaSet e muda a cada rollout.
        var selector = LerSelector(doDeployment);
        if (selector.Length == 0)
            return [];

        var caminho = $"api/v1/namespaces/{Uri.EscapeDataString(ns)}/pods?labelSelector={Uri.EscapeDataString(selector)}";
        var resposta = await LerAsync(caminho, ct);

        var lista = new List<PodInfo>();
        if (resposta.TryGetProperty("items", out var itens))
        {
            foreach (var item in itens.EnumerateArray())
                lista.Add(LerPod(item));
        }

        return lista.OrderBy(p => p.Nome, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // ── Comandos ─────────────────────────────────────────────────────────

    /// <summary>Escala a 0, guardando primeiro as réplicas atuais para o arranque as repor.</summary>
    public async Task<DeploymentInfo> PararAsync(string ns, string nome, CancellationToken ct)
    {
        GarantirComandos();

        var atual = await ObterDeploymentAsync(ns, nome, ct);
        if (atual.ReplicasDesejadas == 0)
            return atual;

        var patch = "{\"metadata\":{\"annotations\":{\"" + AnotacaoReplicas + "\":\""
                  + atual.ReplicasDesejadas + "\"}},\"spec\":{\"replicas\":0}}";

        return await AplicarPatchAsync(ns, nome, patch, ct);
    }

    /// <summary>Repõe as réplicas guardadas ao parar (1 se não houver registo).</summary>
    public async Task<DeploymentInfo> ArrancarAsync(string ns, string nome, CancellationToken ct)
    {
        GarantirComandos();

        var atual = await ObterDeploymentAsync(ns, nome, ct);
        if (atual.ReplicasDesejadas > 0)
            return atual;

        var replicas = atual.ReplicasAntesDeParar is > 0 ? atual.ReplicasAntesDeParar.Value : 1;

        // A anotação é limpa com null: em strategic merge patch é assim que se apaga uma chave.
        // Deixá-la lá faria o portal continuar a mostrar "parado a partir de N" para sempre.
        var patch = "{\"metadata\":{\"annotations\":{\"" + AnotacaoReplicas + "\":null}},"
                  + "\"spec\":{\"replicas\":" + replicas + "}}";

        return await AplicarPatchAsync(ns, nome, patch, ct);
    }

    /// <summary>O mesmo que `kubectl rollout restart`: carimba o template e força um rollout.</summary>
    public async Task<DeploymentInfo> ReiniciarAsync(string ns, string nome, CancellationToken ct)
    {
        GarantirComandos();

        var atual = await ObterDeploymentAsync(ns, nome, ct);
        if (atual.ReplicasDesejadas == 0)
            throw new KubernetesException(
                $"'{nome}' está parado: reiniciar não teria efeito. Use Arrancar.");

        var carimbo = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var patch = "{\"spec\":{\"template\":{\"metadata\":{\"annotations\":{\""
                  + AnotacaoReinicio + "\":\"" + carimbo + "\"}}}}}";

        return await AplicarPatchAsync(ns, nome, patch, ct);
    }

    // ── Log de um pod ────────────────────────────────────────────────────

    /// <summary>
    /// Lê a consola de um pod. Com <paramref name="desde"/> devolve só as linhas posteriores a
    /// esse instante — é o que permite ao portal seguir o log sem repetir o que já mostrou.
    /// </summary>
    public async Task<ResultadoLog> LerLogAsync(
        string ns, string pod, string? contentor, int linhas, string? desde, CancellationToken ct)
    {
        GarantirNamespace(ns);

        var pedidas = Math.Clamp(linhas <= 0 ? 500 : linhas, 1, _opcoes.LinhasLogMaximo);

        var query = new List<string>
        {
            // Sem timestamps não haveria por onde continuar a leitura no pedido seguinte.
            "timestamps=true",
            $"tailLines={pedidas}"
        };

        if (!string.IsNullOrWhiteSpace(contentor))
            query.Add($"container={Uri.EscapeDataString(contentor)}");

        DateTimeOffset? corte = null;
        if (!string.IsNullOrWhiteSpace(desde) && DateTimeOffset.TryParse(desde, out var instante))
        {
            corte = instante;

            // O sinceTime da API do Kubernetes só tem resolução ao segundo, por isso pede-se
            // um segundo a mais e filtra-se a seguir pelo carimbo exato de cada linha.
            var inicio = instante.AddSeconds(-1).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            query.Add($"sinceTime={Uri.EscapeDataString(inicio)}");
        }

        var caminho = $"api/v1/namespaces/{Uri.EscapeDataString(ns)}/pods/{Uri.EscapeDataString(pod)}/log"
                    + "?" + string.Join('&', query);

        var texto = await LerTextoAsync(caminho, ct);

        var resultado = new List<LinhaLog>();
        DateTimeOffset? ultimo = corte;

        foreach (var bruta in texto.Split('\n'))
        {
            var linha = bruta.TrimEnd('\r');
            if (linha.Length == 0) continue;

            // Formato: "<RFC3339Nano> <texto>". Se o carimbo faltar, a linha vai como está —
            // é preferível mostrá-la sem hora do que deitá-la fora.
            var espaco = linha.IndexOf(' ');
            DateTimeOffset? tempo = null;
            var conteudo = linha;

            if (espaco > 0 && DateTimeOffset.TryParse(linha[..espaco], out var carimbo))
            {
                tempo = carimbo;
                conteudo = linha[(espaco + 1)..];

                // "menor que" e não "menor ou igual": várias linhas partilham o mesmo carimbo
                // (o Serilog escreve o JSON e o texto no mesmo instante), e cortar pelo igual
                // perderia as que chegassem depois nesse mesmo instante. As repetições da
                // fronteira são descartadas no portal, que sabe o que já mostrou.
                if (corte is not null && carimbo < corte) continue;

                if (ultimo is null || carimbo > ultimo) ultimo = carimbo;
            }

            resultado.Add(new LinhaLog(tempo, conteudo));
        }

        return new ResultadoLog(resultado, ultimo?.ToString("o"));
    }

    // ── HTTP ─────────────────────────────────────────────────────────────

    private async Task<DeploymentInfo> AplicarPatchAsync(string ns, string nome, string patch, CancellationToken ct)
    {
        GarantirNamespace(ns);

        var caminho = $"apis/apps/v1/namespaces/{Uri.EscapeDataString(ns)}/deployments/{Uri.EscapeDataString(nome)}";

        using var pedido = new HttpRequestMessage(HttpMethod.Patch, caminho)
        {
            // O tipo de conteúdo é que escolhe a semântica do patch. Com application/json o
            // servidor esperaria um JSON Patch (lista de operações) e devolveria 415/400.
            Content = new StringContent(patch, Encoding.UTF8, "application/strategic-merge-patch+json")
        };

        var resposta = await EnviarAsync(pedido, ct);
        return LerDeployment(ns, resposta);
    }

    /// <summary>Igual ao <see cref="LerAsync"/>, mas o corpo é texto: o log não vem em JSON.</summary>
    private async Task<string> LerTextoAsync(string caminho, CancellationToken ct)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, caminho);

        if (http.BaseAddress is null)
            throw new KubernetesException("O endereço do cluster não está configurado (secção Kubernetes).");

        HttpResponseMessage resposta;
        try
        {
            resposta = await http.SendAsync(pedido, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha de ligação ao cluster em {Caminho}.", caminho);
            throw new KubernetesException("Não foi possível contactar o cluster Kubernetes.", ex);
        }

        var corpo = await resposta.Content.ReadAsStringAsync(ct);

        if (!resposta.IsSuccessStatusCode)
            throw new KubernetesException(Explicar(resposta.StatusCode, corpo));

        return corpo;
    }

    private async Task<JsonElement> LerAsync(string caminho, CancellationToken ct)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, caminho);
        return await EnviarAsync(pedido, ct);
    }

    private async Task<JsonElement> EnviarAsync(HttpRequestMessage pedido, CancellationToken ct)
    {
        if (http.BaseAddress is null)
            throw new KubernetesException("O endereço do cluster não está configurado (secção Kubernetes).");

        HttpResponseMessage resposta;
        try
        {
            resposta = await http.SendAsync(pedido, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha de ligação ao cluster em {Caminho}.", pedido.RequestUri);
            throw new KubernetesException("Não foi possível contactar o cluster Kubernetes.", ex);
        }

        var corpo = await resposta.Content.ReadAsStringAsync(ct);

        if (!resposta.IsSuccessStatusCode)
            throw new KubernetesException(Explicar(resposta.StatusCode, corpo));

        try
        {
            using var doc = JsonDocument.Parse(corpo);
            return doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new KubernetesException("O cluster devolveu uma resposta que não é JSON.", ex);
        }
    }

    /// <summary>
    /// Traduz o erro do cluster para algo acionável. O 403 é o caso comum enquanto o RBAC de
    /// kubernetes/rbac/projectmanager-k8s.yaml não estiver aplicado.
    /// </summary>
    private static string Explicar(HttpStatusCode estado, string corpo)
    {
        var detalhe = "";
        try
        {
            using var doc = JsonDocument.Parse(corpo);
            if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                detalhe = m.GetString() ?? "";
        }
        catch (JsonException)
        {
            // Nem todas as respostas de erro são JSON (um proxy pelo meio devolve HTML).
        }

        return estado switch
        {
            HttpStatusCode.Unauthorized =>
                "O cluster recusou as credenciais do portal. O token da ServiceAccount está em falta ou expirou.",
            HttpStatusCode.Forbidden =>
                $"O cluster recusou a operação por falta de permissões. {detalhe}".TrimEnd(),
            HttpStatusCode.NotFound =>
                "O recurso já não existe no cluster.",
            HttpStatusCode.Conflict =>
                $"O cluster recusou a alteração por conflito de versão. Recarregue a lista. {detalhe}".TrimEnd(),
            _ => detalhe.Length > 0
                ? $"O cluster respondeu {(int)estado}: {detalhe}"
                : $"O cluster respondeu {(int)estado}."
        };
    }

    private void GarantirNamespace(string ns)
    {
        // A lista branca é verificada aqui e não só no controlador: qualquer caminho novo que
        // passe pelo gateway fica coberto sem ninguém se lembrar de repetir a validação.
        if (!_opcoes.Namespaces.Contains(ns, StringComparer.OrdinalIgnoreCase))
            throw new NamespaceNaoPermitidoException(ns);
    }

    private void GarantirComandos()
    {
        if (!_opcoes.PermitirComandos)
            throw new KubernetesException("Os comandos sobre deployments estão desligados nesta instalação.");
    }

    // ── Leitura do JSON do cluster ───────────────────────────────────────

    private static DeploymentInfo LerDeployment(string ns, JsonElement item)
    {
        var metadata = Objeto(item, "metadata");
        var spec = Objeto(item, "spec");
        var status = Objeto(item, "status");
        var anotacoes = Objeto(metadata, "annotations");

        var desejadas = InteiroOuZero(spec, "replicas");
        var prontas = InteiroOuZero(status, "readyReplicas");
        var disponiveis = InteiroOuZero(status, "availableReplicas");
        var atualizadas = InteiroOuZero(status, "updatedReplicas");

        int? guardadas = null;
        if (anotacoes.ValueKind == JsonValueKind.Object
            && anotacoes.TryGetProperty(AnotacaoReplicas, out var g)
            && g.ValueKind == JsonValueKind.String
            && int.TryParse(g.GetString(), out var valor))
        {
            guardadas = valor;
        }

        var estado = desejadas == 0
            ? "parado"
            : prontas == 0 ? "degradado"
            : prontas < desejadas || atualizadas < desejadas ? "a-atualizar"
            : "pronto";

        var reinicio = Objeto(Objeto(Objeto(spec, "template"), "metadata"), "annotations");

        return new DeploymentInfo(
            Namespace: ns,
            Nome: TextoOuVazio(metadata, "name"),
            Imagem: PrimeiraImagem(spec),
            ReplicasDesejadas: desejadas,
            ReplicasProntas: prontas,
            ReplicasDisponiveis: disponiveis,
            ReplicasAtualizadas: atualizadas,
            Estado: estado,
            ReplicasAntesDeParar: guardadas,
            Criado: DataOuNulo(metadata, "creationTimestamp"),
            UltimoReinicio: DataOuNulo(reinicio, AnotacaoReinicio));
    }

    private static PodInfo LerPod(JsonElement item)
    {
        var metadata = Objeto(item, "metadata");
        var spec = Objeto(item, "spec");
        var status = Objeto(item, "status");

        var total = 0;
        var prontos = 0;
        var reinicios = 0;
        string? motivo = null;

        if (status.TryGetProperty("containerStatuses", out var contentores)
            && contentores.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in contentores.EnumerateArray())
            {
                total++;
                if (c.TryGetProperty("ready", out var r) && r.ValueKind == JsonValueKind.True) prontos++;
                reinicios += InteiroOuZero(c, "restartCount");

                // A "phase" de um pod em CrashLoopBackOff continua a ser "Running": a razão
                // real só aparece no estado do contentor, e é essa que interessa mostrar.
                if (motivo is null)
                {
                    var estadoContentor = Objeto(c, "state");
                    foreach (var fase in new[] { "waiting", "terminated" })
                    {
                        if (estadoContentor.TryGetProperty(fase, out var detalhe))
                        {
                            var texto = TextoOuVazio(detalhe, "reason");
                            if (texto.Length > 0 && texto != "Completed") motivo = texto;
                            break;
                        }
                    }
                }
            }
        }

        return new PodInfo(
            Nome: TextoOuVazio(metadata, "name"),
            Estado: TextoOuVazio(status, "phase"),
            No: TextoOuVazio(spec, "nodeName"),
            Ip: TextoOuVazio(status, "podIP") is { Length: > 0 } ip ? ip : null,
            Reinicios: reinicios,
            Pronto: total > 0 && prontos == total,
            TotalContentores: total,
            ContentoresProntos: prontos,
            Criado: DataOuNulo(metadata, "creationTimestamp"),
            Motivo: motivo,
            Contentores: LerContentores(spec));
    }

    private static IReadOnlyList<string> LerContentores(JsonElement spec)
    {
        var contentores = Objeto(spec, "containers");
        if (contentores.ValueKind != JsonValueKind.Array)
            return [];

        return contentores.EnumerateArray()
            .Select(c => TextoOuVazio(c, "name"))
            .Where(n => n.Length > 0)
            .ToList();
    }

    private static string LerSelector(JsonElement deployment)
    {
        var matchLabels = Objeto(Objeto(Objeto(deployment, "spec"), "selector"), "matchLabels");
        if (matchLabels.ValueKind != JsonValueKind.Object)
            return "";

        return string.Join(',', matchLabels.EnumerateObject()
            .Where(p => p.Value.ValueKind == JsonValueKind.String)
            .Select(p => $"{p.Name}={p.Value.GetString()}"));
    }

    private static string PrimeiraImagem(JsonElement spec)
    {
        var contentores = Objeto(Objeto(Objeto(spec, "template"), "spec"), "containers");
        if (contentores.ValueKind != JsonValueKind.Array)
            return "";

        foreach (var c in contentores.EnumerateArray())
        {
            var imagem = TextoOuVazio(c, "image");
            if (imagem.Length > 0) return imagem;
        }

        return "";
    }

    private static JsonElement Objeto(JsonElement pai, string nome)
        => pai.ValueKind == JsonValueKind.Object && pai.TryGetProperty(nome, out var filho)
            ? filho
            : default;

    private static string TextoOuVazio(JsonElement pai, string nome)
        => pai.ValueKind == JsonValueKind.Object
           && pai.TryGetProperty(nome, out var valor)
           && valor.ValueKind == JsonValueKind.String
            ? valor.GetString() ?? ""
            : "";

    private static int InteiroOuZero(JsonElement pai, string nome)
        => pai.ValueKind == JsonValueKind.Object
           && pai.TryGetProperty(nome, out var valor)
           && valor.ValueKind == JsonValueKind.Number
           && valor.TryGetInt32(out var numero)
            ? numero
            : 0;

    private static DateTimeOffset? DataOuNulo(JsonElement pai, string nome)
        => DateTimeOffset.TryParse(TextoOuVazio(pai, nome), out var data) ? data : null;
}
