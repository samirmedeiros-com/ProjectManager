using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProjectManagerWebAPI.Models.Kafka;

namespace ProjectManagerWebAPI.Services;

public class KafkaException(string message) : Exception(message);

/// <summary>Endereço do Kafka Connect. Sem credenciais: a REST API do Connect é aberta na rede interna.</summary>
public sealed class KafkaOptions
{
    public const string Seccao = "Kafka";

    /// <summary>REST API do Kafka Connect, ex. http://10.2.2.88:8083</summary>
    public string ConnectUrl { get; set; } = "";

    public int TimeoutSegundos { get; set; } = 30;
}

public interface IKafkaConnectGateway
{
    Task<ResumoKafka> ListarAsync(CancellationToken ct);
    Task<Conector?> ObterAsync(string nome, CancellationToken ct);
    Task<ResultadoReinicio> ReiniciarTaskAsync(string nome, int task, CancellationToken ct);
}

/// <summary>
/// Fala com a REST API do Kafka Connect por HTTP+JSON.
///
/// <para>A listagem usa <c>GET /connectors?expand=status&amp;expand=info</c> — uma chamada só para
/// todos os conectores, estado e configuração. A alternativa (listar nomes e depois pedir o estado
/// de cada um) faria N+1 chamadas para desenhar uma página que se abre a cada minuto.</para>
///
/// <para><b>A configuração do conector traz segredos em claro</b> — <c>connection.password</c> e
/// <c>connection.user</c> vêm no <c>info.config</c>. Deste gateway só saem os três campos que o
/// ecrã mostra (tópico, tabela, classe); o resto nunca chega ao browser. Devolver a configuração
/// inteira "porque o Connect já a dá" seria publicar a password da base a quem abrisse as
/// ferramentas de programador.</para>
/// </summary>
public class KafkaConnectGateway(
    IHttpClientFactory fabrica,
    IOptions<KafkaOptions> opcoes,
    ILogger<KafkaConnectGateway> logger) : IKafkaConnectGateway
{
    private readonly KafkaOptions _opcoes = opcoes.Value;

    private HttpClient Cliente()
    {
        if (string.IsNullOrWhiteSpace(_opcoes.ConnectUrl))
            throw new KafkaException("O endereço do Kafka Connect não está configurado.");

        var http = fabrica.CreateClient("KafkaConnect");
        http.BaseAddress = new Uri(_opcoes.ConnectUrl.TrimEnd('/') + "/");
        http.Timeout = TimeSpan.FromSeconds(_opcoes.TimeoutSegundos);
        return http;
    }

    public async Task<ResumoKafka> ListarAsync(CancellationToken ct)
    {
        using var http = Cliente();

        HttpResponseMessage resposta;
        try
        {
            resposta = await http.GetAsync("connectors?expand=status&expand=info", ct);
        }
        catch (Exception ex)
        {
            // Sem rede para o Connect não há nada a mostrar, e o motivo real poupa meia hora
            // a quem for procurar o problema na aplicação.
            throw new KafkaException($"Não foi possível falar com o Kafka Connect em {_opcoes.ConnectUrl}: {ex.Message}");
        }

        if (!resposta.IsSuccessStatusCode)
            throw new KafkaException($"O Kafka Connect respondeu {(int)resposta.StatusCode} à listagem de conectores.");

        using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct));

        var conectores = doc.RootElement.EnumerateObject()
            .Select(p => LerConector(p.Name, p.Value))
            .OrderBy(c => c.EmErro ? 0 : 1)   // o que está partido fica em cima
            .ThenBy(c => c.Nome, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ResumoKafka
        {
            Conectores = conectores,
            Total = conectores.Count,
            AFuncionar = conectores.Count(c => !c.EmErro && !EhPausa(c.Estado)),
            ComErro = conectores.Count(c => c.EmErro),
            EmPausa = conectores.Count(c => EhPausa(c.Estado)),
        };
    }

    public async Task<Conector?> ObterAsync(string nome, CancellationToken ct)
    {
        using var http = Cliente();

        var resposta = await http.GetAsync($"connectors/{Uri.EscapeDataString(nome)}/status", ct);
        if (resposta.StatusCode == HttpStatusCode.NotFound) return null;
        if (!resposta.IsSuccessStatusCode)
            throw new KafkaException($"O Kafka Connect respondeu {(int)resposta.StatusCode} ao estado de {nome}.");

        using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct));
        return LerEstado(nome, doc.RootElement);
    }

    /// <summary>
    /// Reinicia uma task. O Connect responde <b>204 sem corpo</b> quando aceita — não devolve o
    /// estado novo, que só aparece na listagem seguinte, alguns segundos depois.
    /// </summary>
    public async Task<ResultadoReinicio> ReiniciarTaskAsync(string nome, int task, CancellationToken ct)
    {
        using var http = Cliente();

        var caminho = $"connectors/{Uri.EscapeDataString(nome)}/tasks/{task}/restart";
        var resposta = await http.PostAsync(caminho, content: null, ct);

        if (resposta.IsSuccessStatusCode)
        {
            logger.LogInformation("Task {Task} do conector {Conector} reiniciada", task, nome);
            return new ResultadoReinicio { Conector = nome, Task = task, Sucesso = true };
        }

        var corpo = await resposta.Content.ReadAsStringAsync(ct);
        var motivo = resposta.StatusCode == HttpStatusCode.NotFound
            ? $"O conector {nome} ou a task {task} não existem."
            : $"O Kafka Connect respondeu {(int)resposta.StatusCode}: {Cortar(corpo, 300)}";

        logger.LogWarning("Falha ao reiniciar a task {Task} de {Conector}: {Motivo}", task, nome, motivo);
        return new ResultadoReinicio { Conector = nome, Task = task, Sucesso = false, Mensagem = motivo };
    }

    // ---------------------------------------------------------------- leitura

    private static Conector LerConector(string nome, JsonElement entrada)
    {
        var conector = entrada.TryGetProperty("status", out var estado)
            ? LerEstado(nome, estado)
            : new Conector { Nome = nome };

        if (entrada.TryGetProperty("info", out var info) &&
            info.TryGetProperty("config", out var config))
        {
            // Três campos e mais nenhum — ver o comentário de classe sobre os segredos.
            conector.Topico = Texto(config, "topics") ?? Texto(config, "topic.prefix");
            conector.Tabela = Texto(config, "table.name.format") ?? Texto(config, "table.whitelist");
            conector.Classe = UltimoSegmento(Texto(config, "connector.class"));
        }

        return conector;
    }

    private static Conector LerEstado(string nome, JsonElement estado)
    {
        var conector = new Conector
        {
            Nome = Texto(estado, "name") ?? nome,
            Tipo = Texto(estado, "type") ?? "",
        };

        if (estado.TryGetProperty("connector", out var c))
        {
            conector.Estado = Texto(c, "state") ?? "";
            conector.Worker = Texto(c, "worker_id");
        }

        if (estado.TryGetProperty("tasks", out var tasks) && tasks.ValueKind == JsonValueKind.Array)
        {
            conector.Tasks = tasks.EnumerateArray().Select(t => new TaskConector
            {
                Id = t.TryGetProperty("id", out var id) && id.TryGetInt32(out var valor) ? valor : 0,
                Estado = Texto(t, "state") ?? "",
                Worker = Texto(t, "worker_id"),
                // O traço só existe em tasks falhadas, e é longo: corta-se para o ecrã.
                Traco = Cortar(Texto(t, "trace"), 4000),
            }).OrderBy(t => t.Id).ToList();
        }

        return conector;
    }

    private static bool EhPausa(string? estado) =>
        string.Equals(estado?.Trim(), EstadoKafka.Paused, StringComparison.OrdinalIgnoreCase);

    private static string? Texto(JsonElement objeto, string campo) =>
        objeto.TryGetProperty(campo, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    /// <summary>io.confluent.connect.jdbc.JdbcSinkConnector → JdbcSinkConnector.</summary>
    private static string? UltimoSegmento(string? classe) =>
        string.IsNullOrWhiteSpace(classe) ? classe : classe.Split('.').Last();

    private static string? Cortar(string? texto, int max) =>
        string.IsNullOrEmpty(texto) ? texto : texto.Length <= max ? texto : texto[..max] + "…";
}
