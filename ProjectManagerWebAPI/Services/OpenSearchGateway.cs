using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProjectManagerWebAPI.Models.OpenSearch;

namespace ProjectManagerWebAPI.Services;

/// <summary>
/// Falha vinda do cluster (ou da ligação a ele). O controlador traduz isto num 502 com mensagem
/// legível, para o portal nunca mostrar uma stack trace ao utilizador.
/// </summary>
public sealed class OpenSearchException(string mensagem, Exception? inner = null)
    : Exception(mensagem, inner);

/// <summary>Acesso à API REST do OpenSearch. Sem dependências de cliente oficial — só HTTP + JSON.</summary>
public sealed class OpenSearchGateway(HttpClient http, IOptions<OpenSearchOptions> opcoes, ILogger<OpenSearchGateway> logger)
{
    private readonly OpenSearchOptions _opcoes = opcoes.Value;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ── Estado do cluster ────────────────────────────────────────────────

    public async Task<EstadoCluster> ObterEstadoAsync(CancellationToken ct)
    {
        var raiz = await LerAsync("/", ct);
        var saude = await LerAsync("/_cluster/health", ct);
        var indices = await ListarIndicesAsync(ct);

        return new EstadoCluster(
            Nome: TextoOuVazio(raiz, "cluster_name"),
            Versao: raiz.TryGetProperty("version", out var v) ? TextoOuVazio(v, "number") : "",
            Saude: TextoOuVazio(saude, "status"),
            TotalIndices: indices.Count,
            TotalDocumentos: indices.Sum(i => i.Documentos),
            TamanhoBytes: indices.Sum(i => i.TamanhoBytes));
    }

    // ── Índices ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<IndiceInfo>> ListarIndicesAsync(CancellationToken ct)
    {
        // bytes=b força o tamanho em bytes puros; sem isso vem "3.4gb" e não dá para somar.
        var resposta = await LerAsync("/_cat/indices?format=json&bytes=b&h=health,status,index,docs.count,store.size", ct);

        var lista = new List<IndiceInfo>();
        foreach (var item in resposta.EnumerateArray())
        {
            var nome = TextoOuVazio(item, "index");
            if (nome.Length == 0) continue;
            if (!_opcoes.IncluirIndicesSistema && nome.StartsWith('.')) continue;

            lista.Add(new IndiceInfo(
                Nome: nome,
                Saude: TextoOuVazio(item, "health"),
                Estado: TextoOuVazio(item, "status"),
                Documentos: LongOuZero(item, "docs.count"),
                TamanhoBytes: LongOuZero(item, "store.size")));
        }

        return lista.OrderBy(i => i.Nome, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // ── Campos (mapping) ─────────────────────────────────────────────────

    public async Task<IReadOnlyList<CampoInfo>> ListarCamposAsync(string indice, CancellationToken ct)
    {
        var resposta = await LerAsync($"/{Uri.EscapeDataString(indice)}/_mapping", ct);

        // Um padrão como "logs-*" devolve um objeto por índice concreto; juntamos todos,
        // porque o portal apresenta um único conjunto de campos para a seleção feita.
        var campos = new Dictionary<string, CampoInfo>(StringComparer.Ordinal);
        foreach (var entrada in resposta.EnumerateObject())
        {
            if (!entrada.Value.TryGetProperty("mappings", out var mappings)) continue;
            if (!mappings.TryGetProperty("properties", out var propriedades)) continue;
            AchatarPropriedades(propriedades, prefixo: "", campos);
        }

        return campos.Values.OrderBy(c => c.Nome, StringComparer.Ordinal).ToList();
    }

    private static void AchatarPropriedades(JsonElement propriedades, string prefixo, Dictionary<string, CampoInfo> destino)
    {
        foreach (var propriedade in propriedades.EnumerateObject())
        {
            var nome = prefixo.Length == 0 ? propriedade.Name : $"{prefixo}.{propriedade.Name}";
            var definicao = propriedade.Value;

            // Objetos aninhados: descer sem registar o nó intermédio, que não é pesquisável.
            if (definicao.TryGetProperty("properties", out var filhos))
            {
                AchatarPropriedades(filhos, nome, destino);
                continue;
            }

            var tipo = TextoOuVazio(definicao, "type");
            if (tipo.Length == 0) tipo = "object";

            destino[nome] = new CampoInfo(
                Nome: nome,
                Tipo: tipo,
                // Campos "text" são analisados: ordenar por eles rebenta sem fielddata.
                Ordenavel: tipo != "text",
                Temporal: tipo is "date" or "date_nanos");

            // Sub-campos (tipicamente "campo.keyword") — é por aí que se ordena um text.
            if (definicao.TryGetProperty("fields", out var subcampos))
                AchatarPropriedades(subcampos, nome, destino);
        }
    }

    // ── Pesquisa ─────────────────────────────────────────────────────────

    public async Task<ResultadoPesquisa> PesquisarAsync(PedidoPesquisa pedido, CancellationToken ct)
    {
        var tamanho = Math.Clamp(pedido.Tamanho, 1, _opcoes.TamanhoPaginaMaximo);
        var pagina = Math.Max(pedido.Pagina, 0);

        var corpo = ConstruirCorpoPesquisa(pedido, pagina, tamanho);
        var resposta = await EnviarAsync(HttpMethod.Post, $"/{Uri.EscapeDataString(pedido.Indice)}/_search", corpo, ct);

        var hits = resposta.GetProperty("hits");
        var total = hits.GetProperty("total");

        var documentos = new List<DocumentoResultado>();
        var colunas = new List<string>();
        var vistas = new HashSet<string>(StringComparer.Ordinal);

        foreach (var hit in hits.GetProperty("hits").EnumerateArray())
        {
            var campos = new Dictionary<string, JsonElement?>(StringComparer.Ordinal);
            if (hit.TryGetProperty("_source", out var source) && source.ValueKind == JsonValueKind.Object)
                AchatarDocumento(source, prefixo: "", campos);

            // A ordem das colunas segue a ordem de aparição no primeiro documento que as traz,
            // que é mais previsível para o utilizador do que a ordem alfabética.
            foreach (var chave in campos.Keys)
                if (vistas.Add(chave)) colunas.Add(chave);

            documentos.Add(new DocumentoResultado(
                Id: TextoOuVazio(hit, "_id"),
                Indice: TextoOuVazio(hit, "_index"),
                Score: hit.TryGetProperty("_score", out var s) && s.ValueKind == JsonValueKind.Number
                    ? s.GetDouble()
                    : null,
                Campos: campos));
        }

        return new ResultadoPesquisa(
            Total: total.GetProperty("value").GetInt64(),
            TotalExato: TextoOuVazio(total, "relation") == "eq",
            DuracaoMs: resposta.TryGetProperty("took", out var took) ? took.GetInt64() : 0,
            Colunas: colunas,
            Documentos: documentos);
    }

    private static object ConstruirCorpoPesquisa(PedidoPesquisa pedido, int pagina, int tamanho)
    {
        var filtros = new List<object>();

        if (!string.IsNullOrWhiteSpace(pedido.CampoData) && (pedido.De is not null || pedido.Ate is not null))
        {
            var intervalo = new Dictionary<string, object>();
            if (pedido.De is not null) intervalo["gte"] = pedido.De.Value.ToUniversalTime();
            if (pedido.Ate is not null) intervalo["lte"] = pedido.Ate.Value.ToUniversalTime();
            filtros.Add(new { range = new Dictionary<string, object> { [pedido.CampoData!] = intervalo } });
        }

        var texto = pedido.Consulta?.Trim();
        object consulta = string.IsNullOrEmpty(texto) || texto == "*"
            ? new { match_all = new { } }
            // query_string aceita a sintaxe Lucene que o utilizador já conhece do Discover;
            // lenient evita 400 quando o termo não converte para o tipo do campo.
            : new
            {
                query_string = new
                {
                    query = texto,
                    analyze_wildcard = true,
                    lenient = true,
                    default_operator = "AND",
                },
            };

        var corpo = new Dictionary<string, object>
        {
            ["from"] = pagina * tamanho,
            ["size"] = tamanho,
            ["track_total_hits"] = true,
            ["query"] = new { @bool = new { must = new[] { consulta }, filter = filtros } },
        };

        var ordenarPor = pedido.OrdenarPor ?? pedido.CampoData;
        if (!string.IsNullOrWhiteSpace(ordenarPor))
        {
            corpo["sort"] = new[]
            {
                new Dictionary<string, object>
                {
                    [ordenarPor] = new
                    {
                        order = pedido.OrdemDescendente ? "desc" : "asc",
                        // Documentos sem o campo não devem rebentar a ordenação.
                        missing = "_last",
                        unmapped_type = "keyword",
                    },
                },
            };
        }

        return corpo;
    }

    private static void AchatarDocumento(JsonElement objeto, string prefixo, Dictionary<string, JsonElement?> destino)
    {
        foreach (var propriedade in objeto.EnumerateObject())
        {
            var nome = prefixo.Length == 0 ? propriedade.Name : $"{prefixo}.{propriedade.Name}";

            // Só se desce em objetos. Arrays ficam inteiros numa célula — achatar
            // "tags.0", "tags.1" geraria colunas diferentes por documento.
            if (propriedade.Value.ValueKind == JsonValueKind.Object)
                AchatarDocumento(propriedade.Value, nome, destino);
            else
                destino[nome] = propriedade.Value.Clone();
        }
    }

    // ── HTTP ─────────────────────────────────────────────────────────────

    private Task<JsonElement> LerAsync(string caminho, CancellationToken ct)
        => EnviarAsync(HttpMethod.Get, caminho, corpo: null, ct);

    private async Task<JsonElement> EnviarAsync(HttpMethod metodo, string caminho, object? corpo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opcoes.BaseUrl))
            throw new OpenSearchException("O endereço do OpenSearch não está configurado (OpenSearch:BaseUrl).");

        using var pedido = new HttpRequestMessage(metodo, caminho);
        if (corpo is not null)
            pedido.Content = JsonContent.Create(corpo, options: Json);

        HttpResponseMessage resposta;
        try
        {
            resposta = await http.SendAsync(pedido, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Falha de ligação ao OpenSearch em {BaseUrl}.", _opcoes.BaseUrl);
            throw new OpenSearchException($"Não foi possível contactar o OpenSearch em {_opcoes.BaseUrl}.", ex);
        }

        using (resposta)
        {
            var conteudo = await resposta.Content.ReadAsStringAsync(ct);

            if (!resposta.IsSuccessStatusCode)
                throw new OpenSearchException(DescreverErro(resposta.StatusCode, conteudo));

            try
            {
                // Clone() para o JsonDocument poder ser libertado sem invalidar o que devolvemos.
                using var documento = JsonDocument.Parse(conteudo);
                return documento.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                throw new OpenSearchException("O OpenSearch devolveu uma resposta que não é JSON válido.", ex);
            }
        }
    }

    private static string DescreverErro(System.Net.HttpStatusCode estado, string conteudo)
    {
        // O OpenSearch devolve {"error":{"reason":"..."}} — é a única parte útil para o utilizador.
        try
        {
            using var documento = JsonDocument.Parse(conteudo);
            if (documento.RootElement.TryGetProperty("error", out var erro))
            {
                var razao = erro.ValueKind == JsonValueKind.Object
                    ? TextoOuVazio(erro, "reason")
                    : erro.ToString();
                if (!string.IsNullOrWhiteSpace(razao))
                    return $"O OpenSearch recusou o pedido ({(int)estado}): {razao}";
            }
        }
        catch (JsonException)
        {
            // Corpo não-JSON (proxy, HTML de erro) — cai para a mensagem genérica.
        }

        return estado == System.Net.HttpStatusCode.Unauthorized
            ? "Credenciais inválidas ou em falta para o OpenSearch (401)."
            : $"O OpenSearch devolveu o estado {(int)estado}.";
    }

    // ── Leitura defensiva de JSON ────────────────────────────────────────

    private static string TextoOuVazio(JsonElement elemento, string propriedade)
        => elemento.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString() ?? ""
            : "";

    private static long LongOuZero(JsonElement elemento, string propriedade)
    {
        if (!elemento.TryGetProperty(propriedade, out var valor)) return 0;
        return valor.ValueKind switch
        {
            JsonValueKind.Number => valor.GetInt64(),
            // O _cat devolve tudo como string, incluindo os números.
            JsonValueKind.String => long.TryParse(valor.GetString(), out var n) ? n : 0,
            _ => 0,
        };
    }
}
