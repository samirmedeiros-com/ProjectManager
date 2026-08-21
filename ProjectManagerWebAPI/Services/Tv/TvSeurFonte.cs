using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services.Tv;

/// <summary>
/// Operação SEUR do dia: guias criadas, envio para o Atlas e erros recentes.
///
/// A SEUR_GUIA é uma tabela de produção grande e cada varredura da janela do dia
/// custa cerca de 4 segundos — medido. Por isso **tudo o que vem dela sai de uma
/// só query**: traz-se um punhado de colunas de ontem e de hoje (umas milhares de
/// linhas) e agrega-se em memória. A versão anterior fazia oito varreduras para
/// os mesmos números e demorava 32 segundos.
///
/// Nota do stack, duas armadilhas do Oracle aqui: nada de <c>AnyAsync()</c>, que
/// traduz para literais booleanos inexistentes (ORA-00904); e nada de <c>??</c>
/// dentro da query, que vira COALESCE(VARCHAR2, N'literal') e dá ORA-12704.
/// </summary>
public class TvSeurFonte : ITvFonte
{
    public string Chave => "seur";

    private readonly ApplicationDbContext _db;
    private readonly TvDashboardOptions _opcoes;

    public TvSeurFonte(ApplicationDbContext db, IOptions<TvDashboardOptions> opcoes)
    {
        _db = db;
        _opcoes = opcoes.Value;
    }

    public async Task<object> ObterAsync(CancellationToken ct)
    {
        var hoje = DateTime.Today;
        var amanha = hoje.AddDays(1);
        var ontem = hoje.AddDays(-1);
        var limite = Math.Max(1, _opcoes.LimiteLinhas);

        // A única varredura à SEUR_GUIA. Cobre ontem e hoje para dar a comparação
        // sem pagar uma segunda passagem.
        var guias = await _db.SeurGuias.AsNoTracking()
            .Where(g => g.DtCriacao >= ontem && g.DtCriacao < amanha)
            .Select(g => new { g.DtCriacao, g.FlagAtlas, g.ContaDpd, g.QtdVolumes })
            .ToListAsync(ct);

        var deHoje = guias.Where(g => g.DtCriacao >= hoje).ToList();
        var deOntem = guias.Count - deHoje.Count;

        // Flag do envio para o Atlas: Y enviado, E erro, N por enviar.
        var enviadas = deHoje.Count(g => g.FlagAtlas == "Y");
        var comErro = deHoje.Count(g => g.FlagAtlas == "E");
        var porEnviar = deHoje.Count(g => g.FlagAtlas == "N");

        // Guias por hora até à hora atual, para se ver o ritmo e detetar uma paragem.
        var porHoraBruto = deHoje.GroupBy(g => g.DtCriacao.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var porHora = Enumerable.Range(0, DateTime.Now.Hour + 1)
            .Select(h => new TvFatiaDto
            {
                Rotulo = h.ToString("00") + "h",
                Total = porHoraBruto.TryGetValue(h, out var n) ? n : 0
            })
            .ToList();

        var topContas = deHoje.GroupBy(g => g.ContaDpd)
            .Select(g => new TvFatiaDto { Rotulo = g.Key, Total = g.Count() })
            .OrderByDescending(f => f.Total)
            .Take(limite)
            .ToList();

        // A SEUR_ERRO e a VERIFY_SEUR são pequenas — medidas em ~100ms cada.
        var errosHoje = _db.SeurErros.AsNoTracking()
            .Where(e => e.DatahoraInsert >= hoje && e.DatahoraInsert < amanha);

        var erros = await errosHoje
            .OrderByDescending(e => e.DatahoraInsert)
            .Select(e => new { e.Referencia, e.Title, e.Detail, e.DatahoraInsert })
            .ToListAsync(ct);

        var errosPorTipo = erros
            .GroupBy(e => e.Title ?? "Sem título")
            .Select(g => new TvFatiaDto { Rotulo = g.Key, Total = g.Count() })
            .OrderByDescending(f => f.Total)
            .Take(limite)
            .ToList();

        var ultimosErros = erros
            .Take(limite)
            .Select(e => new TvErroDto
            {
                Referencia = e.Referencia ?? "—",
                Titulo = e.Title ?? "Sem título",
                Detalhe = e.Detail,
                Quando = e.DatahoraInsert
            })
            .ToList();

        // Verify pendente: guias entregues ao Verify que ainda não têm resposta.
        var verifyPendente = await _db.SeurVerifies.AsNoTracking()
            .CountAsync(v => v.DatahoraInsert >= hoje && v.DatahoraInsert < amanha && v.VerifyFlag == "N", ct);

        var recolhasPorHora = await ContarRecolhasPorHoraAsync(ct);

        return new TvSeurDto
        {
            GuiasHoje = deHoje.Count,
            GuiasOntem = deOntem,
            Enviadas = enviadas,
            ComErro = comErro,
            PorEnviar = porEnviar,
            Volumes = deHoje.Sum(g => g.QtdVolumes ?? 0),
            // Sem guias no dia a taxa é 100%: zero de zero falhado não é uma avaria.
            TaxaEnvio = deHoje.Count == 0 ? 100 : (int)Math.Round(100.0 * enviadas / deHoje.Count),
            VerifyPendente = verifyPendente,
            ErrosHoje = erros.Count,
            RecolhasHoje = recolhasPorHora.Sum(f => f.Total),
            RecolhasPorHora = recolhasPorHora,
            GuiasPorHora = porHora,
            ErrosPorTipo = errosPorTipo,
            TopContas = topContas,
            UltimosErros = ultimosErros
        };
    }

    /// <summary>
    /// Recolhas marcadas hoje em Portugal, por hora de marcação.
    ///
    /// A CWPICKUPS2_TAB não tem DbSet nem entidade — é uma tabela do CHRONO_WEB,
    /// fora do modelo desta aplicação, e uma query directa evita arrastar uma
    /// entidade com 48 colunas só para contar linhas.
    ///
    /// **A hora vem do DATA_INSERT, não do PICKUP_DATE**: o PICKUP_DATE é o dia
    /// em que a recolha vai acontecer e não tem hora; a pergunta aqui é quantas
    /// foram marcadas hoje e a que ritmo.
    ///
    /// Custo, medido: ~1,5s. Não há índice por DATA_INSERT nesta tabela de 2,9
    /// milhões de linhas, portanto é varredura completa — a mesma ordem de
    /// grandeza da varredura à SEUR_GUIA que já se faz acima. Se um dia pesar,
    /// o caminho é um índice em DATA_INSERT, não partir isto em várias queries.
    ///
    /// O prefixo X do PICKUPNUMBER não é o marcador de país: G e T também são PT.
    /// Distingue a origem da marcação, e é o conjunto que se quer ver no mural.
    /// </summary>
    private async Task<List<TvFatiaDto>> ContarRecolhasPorHoraAsync(CancellationToken ct)
    {
        var hoje = DateTime.Today;

        var ligacao = _db.Database.GetDbConnection();
        if (ligacao.State != ConnectionState.Open) await ligacao.OpenAsync(ct);

        using var cmd = ligacao.CreateCommand();
        cmd.CommandText = """
            SELECT TO_CHAR(DATA_INSERT, 'HH24'), COUNT(*)
            FROM CHRONO_WEB.CWPICKUPS2_TAB
            WHERE DATA_INSERT >= :p0 AND DATA_INSERT < :p1
              AND PICKUPNUMBER LIKE 'X%'
              AND PICKUP_COUNTRY = 'PT'
            GROUP BY TO_CHAR(DATA_INSERT, 'HH24')
            """;
        cmd.CommandTimeout = 120;

        // Os limites vêm do relógio da aplicação, como no resto desta fonte, e não
        // de TRUNC(SYSDATE) — assim o card cobre o mesmo dia que os cards ao lado.
        AdicionarParametro(cmd, "p0", hoje);
        AdicionarParametro(cmd, "p1", hoje.AddDays(1));

        var porHoraBruto = new Dictionary<int, int>();
        using (var leitor = await cmd.ExecuteReaderAsync(ct))
        {
            while (await leitor.ReadAsync(ct))
                porHoraBruto[int.Parse(leitor.GetString(0))] = Convert.ToInt32(leitor.GetValue(1));
        }

        // Horas sem marcações aparecem a zero: um buraco a meio da manhã é
        // informação, e sem a hora vazia o gráfico esconde-o.
        return Enumerable.Range(0, DateTime.Now.Hour + 1)
            .Select(h => new TvFatiaDto
            {
                Rotulo = h.ToString("00") + "h",
                Total = porHoraBruto.TryGetValue(h, out var n) ? n : 0
            })
            .ToList();
    }

    private static void AdicionarParametro(System.Data.Common.DbCommand cmd, string nome, DateTime valor)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = nome;
        p.DbType = DbType.Date;
        p.Value = valor;
        cmd.Parameters.Add(p);
    }
}
