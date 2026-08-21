using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services.Tv;

/// <summary>
/// Compara o que existe no AS400 (OPERACOES.HHP000) com o que já chegou ao Oracle
/// (CHRONO_WEB.CW_PT_AS400_NEW_PORTAL), por dia, pelo campo HHPDAT que ambos têm.
///
/// **Os HHPROWID repetidos são excluídos dos dois lados**, e não por preciosismo:
/// o AS400 tem repetidos (umas dezenas por dia) e o Oracle não. Comparar totais em
/// bruto mostraria uma diferença que não existe — o número que interessa é quantos
/// registos distintos estão de um lado e não do outro.
///
/// Custo, medido: o dia corrente custa ~5s no AS400 e ~1s no Oracle; os quatro dias
/// de uma vez custam ~43s no AS400. Como os dias fechados já não mudam, são pedidos
/// uma vez e guardados durante horas — só o dia corrente é recalculado a cada ciclo.
///
/// Ligação ao AS400: JDBC pelo jt400, o driver oficial da IBM, compilado para .NET
/// pelo IKVM (ver o .csproj). Preferiu-se ao ODBC porque não exige o IBM i Access
/// Driver nem o unixODBC instalados na máquina — funciona igual em desenvolvimento
/// e em produção, sem passos de instalação no servidor. Os database links do Oracle
/// para o AS400 também foram tentados e estão obsoletos (ORA-12154).
///
/// Configuração em As400: Host, Utilizador e Password. Se o AS400 não responder, a
/// fonte devolve os números do Oracle e marca As400Disponivel = false — o ecrã diz
/// que a comparação está cega em vez de mostrar zeros que parecem dados.
/// </summary>
public class TvAs400Fonte : ITvFonte
{
    public string Chave => "as400";

    private const string ChaveCacheFechados = "tv:as400:dias-fechados";

    /// <summary>Quantos dias fechados acompanham o dia corrente.</summary>
    private const int DiasAnteriores = 3;

    /// <summary>Ligação ao AS400 quando o appsettings da máquina não traz a secção As400.</summary>
    private const string HostPorOmissao = "192.168.239.26";
    private const string UtilizadorPorOmissao = "oracle";
    private const string PasswordPorOmissao = "oracle";

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;

    public TvAs400Fonte(ApplicationDbContext db, IMemoryCache cache, IConfiguration config)
    {
        _db = db;
        _cache = cache;
        _config = config;
    }

    private sealed record Contagem(int Total, int Unicos);

    public async Task<object> ObterAsync(CancellationToken ct)
    {
        var hoje = DateTime.Today;
        var dias = Enumerable.Range(0, DiasAnteriores + 1)
            .Select(i => int.Parse(hoje.AddDays(-i).ToString("yyyyMMdd")))
            .ToList();

        var diaCorrente = dias[0];
        var fechados = dias.Skip(1).ToList();

        var oracle = new Dictionary<int, Contagem>();
        var as400 = new Dictionary<int, Contagem>();
        var as400Ok = true;

        // Os dias fechados já não mudam: pedidos uma vez, guardados até ao fim do dia.
        var cacheFechados = await _cache.GetOrCreateAsync($"{ChaveCacheFechados}:{diaCorrente}", async entrada =>
        {
            entrada.AbsoluteExpiration = new DateTimeOffset(hoje.AddDays(1));

            var ora = await ContarNoOracleAsync(fechados, ct);
            var (as4, ok) = await ContarNoAs400Async(fechados, ct);
            return (Oracle: ora, As400: as4, Ok: ok);
        });

        foreach (var (dia, c) in cacheFechados.Oracle) oracle[dia] = c;
        foreach (var (dia, c) in cacheFechados.As400) as400[dia] = c;
        as400Ok &= cacheFechados.Ok;

        var oraHoje = await ContarNoOracleAsync([diaCorrente], ct);
        foreach (var (dia, c) in oraHoje) oracle[dia] = c;

        var (as4Hoje, okHoje) = await ContarNoAs400Async([diaCorrente], ct);
        foreach (var (dia, c) in as4Hoje) as400[dia] = c;
        as400Ok &= okHoje;

        var resultado = dias.Select(dia =>
        {
            var o = oracle.TryGetValue(dia, out var vo) ? vo : new Contagem(0, 0);
            var a = as400.TryGetValue(dia, out var va) ? va : new Contagem(0, 0);

            return new TvAs400DiaDto
            {
                Dia = dia,
                Rotulo = Rotulo(dia, hoje),
                As400 = a.Unicos,
                As400Duplicados = a.Total - a.Unicos,
                Oracle = o.Unicos,
                OracleDuplicados = o.Total - o.Unicos,
                // Só conta o que falta ao Oracle. Se o Oracle tiver mais (acontece
                // quando chega um registo enquanto as duas contagens correm), a
                // diferença é ruído de medição e não uma falha de sincronização.
                EmFalta = Math.Max(0, a.Unicos - o.Unicos),
                // Floor e não Round: com 413 registos por replicar em 376 mil, o
                // arredondamento dava 100% e o ecrã dizia que estava tudo em dia.
                Cobertura = a.Unicos == 0 ? 100 : (int)Math.Floor(100.0 * Math.Min(o.Unicos, a.Unicos) / a.Unicos)
            };
        }).ToList();

        return new TvAs400Dto { Dias = resultado, As400Disponivel = as400Ok };
    }

    private static string Rotulo(int yyyymmdd, DateTime hoje)
    {
        if (yyyymmdd == int.Parse(hoje.ToString("yyyyMMdd"))) return "Hoje";
        if (yyyymmdd == int.Parse(hoje.AddDays(-1).ToString("yyyyMMdd"))) return "Ontem";
        return $"{yyyymmdd % 100:00}/{yyyymmdd / 100 % 100:00}";
    }

    private async Task<Dictionary<int, Contagem>> ContarNoOracleAsync(List<int> dias, CancellationToken ct)
    {
        var resultado = new Dictionary<int, Contagem>();
        if (dias.Count == 0) return resultado;

        var ligacao = _db.Database.GetDbConnection();
        if (ligacao.State != ConnectionState.Open) await ligacao.OpenAsync(ct);

        using var cmd = ligacao.CreateCommand();
        cmd.CommandText = $"""
            SELECT HHPDAT, COUNT(*), COUNT(DISTINCT HHPROWID)
            FROM CHRONO_WEB.CW_PT_AS400_NEW_PORTAL
            WHERE HHPDAT BETWEEN {dias.Min()} AND {dias.Max()}
            GROUP BY HHPDAT
            """;
        cmd.CommandTimeout = 180;

        using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
            resultado[Convert.ToInt32(leitor.GetValue(0))] =
                new Contagem(Convert.ToInt32(leitor.GetValue(1)), Convert.ToInt32(leitor.GetValue(2)));

        return resultado;
    }

    private async Task<(Dictionary<int, Contagem> Contagens, bool Ok)> ContarNoAs400Async(List<int> dias, CancellationToken ct)
    {
        var resultado = new Dictionary<int, Contagem>();
        if (dias.Count == 0) return (resultado, true);

        // Valores de origem no código, com o appsettings a sobrepor-se quando traz
        // a secção As400. O deploy é a cópia da publicação para o servidor e o
        // ficheiro de configuração que lá está é anterior ao mural: sem estes
        // valores, a comparação com o Oracle chegava a produção cega.
        var host = _config["As400:Host"] ?? HostPorOmissao;
        var utilizador = _config["As400:Utilizador"] ?? UtilizadorPorOmissao;
        var password = _config["As400:Password"] ?? PasswordPorOmissao;

        if (string.IsNullOrWhiteSpace(host))
        {
            Console.WriteLine("[TV] AS400 sem Host configurado na secção As400.");
            return (resultado, false);
        }

        var sql = $"""
            SELECT HHPDAT, COUNT(*), COUNT(DISTINCT HHPROWID)
            FROM OPERACOES.HHP000
            WHERE HHPDAT BETWEEN {dias.Min()} AND {dias.Max()}
            GROUP BY HHPDAT
            """;

        try
        {
            // O jt400 é síncrono e bloqueia; fora do thread pool do pedido para não
            // prender um worker durante os segundos que a consulta demora.
            var contagens = await Task.Run(() => ConsultarAs400(host, utilizador, password, sql), ct);
            foreach (var (dia, c) in contagens) resultado[dia] = c;
            return (resultado, true);
        }
        catch (Exception ex)
        {
            // O AS400 em baixo não pode derrubar o bloco: os números do Oracle
            // continuam a valer, e o ecrã assinala que a comparação está cega.
            Console.WriteLine($"[TV] AS400 indisponível: {ex.Message}");
            return (resultado, false);
        }
    }

    private static Dictionary<int, Contagem> ConsultarAs400(string host, string? utilizador, string? password, string sql)
    {
        var contagens = new Dictionary<int, Contagem>();

        // Com o IKVM a classe vem referenciada diretamente — não há classpath a
        // percorrer, por isso o driver regista-se em vez de ser procurado.
        java.sql.DriverManager.registerDriver(new com.ibm.as400.access.AS400JDBCDriver());

        var url = $"jdbc:as400://{host};prompt=false";
        var ligacao = java.sql.DriverManager.getConnection(url, utilizador, password);

        try
        {
            var st = ligacao.createStatement();
            st.setQueryTimeout(180);

            var r = st.executeQuery(sql);
            while (r.next())
                contagens[r.getInt(1)] = new Contagem(r.getInt(2), r.getInt(3));

            r.close();
            st.close();
        }
        finally
        {
            ligacao.close();
        }

        return contagens;
    }
}
