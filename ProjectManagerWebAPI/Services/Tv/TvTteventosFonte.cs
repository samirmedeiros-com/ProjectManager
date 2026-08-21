using System.Data;
using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services.Tv;

/// <summary>
/// Eventos de tracking da DPDIT.GEODT01TT, hoje e ontem.
///
/// A tabela tem 27,8 milhões de linhas e não está mapeada no DbContext — daí SQL
/// direto. Uma só passagem sobre a janela de dois dias serve tudo: agrupa por dia
/// **e** pela hora de envio, o que dá ~50 linhas de resultado a partir das quais
/// se derivam os totais diários e o gráfico horário, sem segunda varredura.
///
/// Semântica dos campos, confirmada nos dados:
/// - `FLAGENV`: Y enviado com sucesso, E erro no envio, N ainda na fila.
/// - `DATAFLAG_ENV`: carimbo de quando o envio foi tentado. Está preenchido nos Y
///   e nos E, e nulo nos N — por isso é este, e não `DATAHORA_INSERT`, que diz a
///   que horas as coisas saíram. (`DATAHORA_ENVIO` existe mas está sempre nula.)
/// </summary>
public class TvTteventosFonte : ITvFonte
{
    public string Chave => "tteventos";

    private const string Sql = """
        SELECT TRUNC(DATAHORA_INSERT) AS dia,
               TO_CHAR(DATAFLAG_ENV,'HH24') AS hora_envio,
               COUNT(*) AS total,
               SUM(CASE WHEN FLAGENV = 'Y' THEN 1 ELSE 0 END) AS sucesso,
               SUM(CASE WHEN FLAGENV = 'E' THEN 1 ELSE 0 END) AS erro,
               SUM(CASE WHEN FLAGENV = 'N' THEN 1 ELSE 0 END) AS por_enviar,
               SUM(CASE WHEN FLAGENV IS NULL OR FLAGENV NOT IN ('Y','E','N') THEN 1 ELSE 0 END) AS outros
        FROM DPDIT.GEODT01TT
        WHERE DATAHORA_INSERT >= TRUNC(SYSDATE) - 1
        GROUP BY TRUNC(DATAHORA_INSERT), TO_CHAR(DATAFLAG_ENV,'HH24')
        """;

    /// <summary>
    /// Estado da fila, sobre a tabela toda, repartido por tipo de evento. Agrupar
    /// por SCANCODEX dá algumas dezenas de linhas, das quais se derivam também o
    /// total e o mais antigo — uma passagem em vez de duas.
    /// </summary>
    private const string SqlFila = """
        SELECT SCANCODEX,
               COUNT(*) AS pendentes,
               MIN(DATAHORA_INSERT) AS mais_antigo,
               ROUND((SYSDATE - MIN(DATAHORA_INSERT)) * 24 * 60) AS atraso_minutos
        FROM DPDIT.GEODT01TT
        WHERE FLAGENV = 'N'
        GROUP BY SCANCODEX
        """;

    private readonly ApplicationDbContext _db;

    public TvTteventosFonte(ApplicationDbContext db) => _db = db;

    private sealed record Balde(DateTime Dia, string? Hora, int Total, int Sucesso, int Erro, int PorEnviar, int Outros);

    public async Task<object> ObterAsync(CancellationToken ct)
    {
        var ligacao = _db.Database.GetDbConnection();
        if (ligacao.State != ConnectionState.Open) await ligacao.OpenAsync(ct);

        using var cmd = ligacao.CreateCommand();
        cmd.CommandText = Sql;
        cmd.CommandTimeout = 120;

        var baldes = new List<Balde>();
        using (var leitor = await cmd.ExecuteReaderAsync(ct))
        {
            while (await leitor.ReadAsync(ct))
                baldes.Add(new Balde(
                    leitor.GetDateTime(0),
                    leitor.IsDBNull(1) ? null : leitor.GetString(1),
                    Num(leitor, 2), Num(leitor, 3), Num(leitor, 4), Num(leitor, 5), Num(leitor, 6)));
        }

        var hoje = DateTime.Today;

        var dias = baldes
            .GroupBy(b => b.Dia)
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                var sucesso = g.Sum(b => b.Sucesso);
                var erro = g.Sum(b => b.Erro);
                var tentados = sucesso + erro;

                return new TvTteventosDiaDto
                {
                    Dia = g.Key,
                    Rotulo = g.Key == hoje ? "Hoje" : g.Key == hoje.AddDays(-1) ? "Ontem" : g.Key.ToString("dd/MM"),
                    Total = g.Sum(b => b.Total),
                    Sucesso = sucesso,
                    Erro = erro,
                    PorEnviar = g.Sum(b => b.PorEnviar),
                    OutrosEstados = g.Sum(b => b.Outros),
                    // A taxa mede-se sobre o que já foi tentado: contar a fila por
                    // enviar como insucesso faria a percentagem cair só por haver
                    // trabalho pendente, que é o estado normal a meio do dia.
                    TaxaSucesso = tentados == 0 ? 100 : (int)Math.Round(100.0 * sucesso / tentados)
                };
            })
            .ToList();

        // Só as horas já decorridas: uma cauda de barras a zero até às 23h faria
        // um dia normal parecer um dia parado.
        var deHoje = baldes.Where(b => b.Dia == hoje && b.Hora is not null).ToList();

        var porHora = Enumerable.Range(0, DateTime.Now.Hour + 1)
            .Select(h =>
            {
                var b = deHoje.FirstOrDefault(x => x.Hora == h.ToString("00"));
                return new TvTteventosHoraDto
                {
                    Rotulo = h.ToString("00") + "h",
                    Enviados = b?.Total ?? 0,
                    Sucesso = b?.Sucesso ?? 0,
                    Erro = b?.Erro ?? 0
                };
            })
            .ToList();

        return new TvTteventosDto { Dias = dias, PorHora = porHora, Fila = await ObterFilaAsync(ligacao, ct) };
    }

    private static async Task<TvTteventosFilaDto> ObterFilaAsync(System.Data.Common.DbConnection ligacao, CancellationToken ct)
    {
        using var cmd = ligacao.CreateCommand();
        cmd.CommandText = SqlFila;
        cmd.CommandTimeout = 60;

        var linhas = new List<(string Codigo, int Total, DateTime? MaisAntigo, int Atraso)>();

        using (var leitor = await cmd.ExecuteReaderAsync(ct))
        {
            while (await leitor.ReadAsync(ct))
                linhas.Add((
                    leitor.IsDBNull(0) ? "—" : leitor.GetString(0).Trim(),
                    Num(leitor, 1),
                    leitor.IsDBNull(2) ? null : leitor.GetDateTime(2),
                    Num(leitor, 3)));
        }

        if (linhas.Count == 0) return new TvTteventosFilaDto();

        // O atraso do conjunto é o do grupo que espera há mais tempo.
        var maisAntigo = linhas.Where(l => l.MaisAntigo.HasValue).OrderBy(l => l.MaisAntigo).FirstOrDefault();
        var maior = linhas.OrderByDescending(l => l.Total).First();

        return new TvTteventosFilaDto
        {
            Pendentes = linhas.Sum(l => l.Total),
            MaisAntigoChegouEm = maisAntigo.MaisAntigo,
            AtrasoMinutos = maisAntigo.Atraso,
            EventoComMais = maior.Codigo,
            EventoComMaisTotal = maior.Total,
            PorEvento = linhas
                .OrderByDescending(l => l.Total)
                .Take(5)
                .Select(l => new TvFatiaDto { Rotulo = l.Codigo, Total = l.Total })
                .ToList()
        };
    }

    private static int Num(IDataRecord r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
}
