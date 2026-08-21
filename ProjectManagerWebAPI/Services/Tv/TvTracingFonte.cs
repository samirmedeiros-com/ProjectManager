using System.Data;
using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services.Tv;

/// <summary>
/// Tracing público, da CHRONO_WEB.CW_PT_AS400_NEW_PORTAL.
///
/// É de longe a maior tabela do mural: **398 milhões de linhas e 232 GB**. Duas
/// armadilhas descobertas ao construir isto, ambas caras de aprender à segunda:
///
/// 1. **`DATAINSERT` não serve de janela** — é nulo em 265 dos 398 milhões de
///    linhas. Filtrar por lá varre o índice quase todo. A coluna de data útil é a
///    `HHPDATINC`, um número em formato YYYYMMDD, com índice próprio.
/// 2. **`MIN` e `MAX` na mesma consulta** anulam a otimização de pontas do índice
///    e passam a full scan. Numa tabela desta dimensão isso não devolve.
///
/// Significado das flags, confirmado nos dados:
/// - `FLAGDPDGO`: 'Y' foi enviado ao DPD Go (tem sempre `DATAHORA_DPDGO`), nulo
///   está pendente (nunca tem carimbo). Não existe 'N' nesta coluna.
/// - `HHPCONFIRM`: 'Y' enviado ao Portal, 'N' pendente, 'E' erro.
/// </summary>
public class TvTracingFonte : ITvFonte
{
    public string Chave => "tracing";

    private const string Sql = """
        SELECT HHPDATINC AS dia,
               COUNT(*) AS total,
               SUM(CASE WHEN FLAGDPDGO = 'Y' THEN 1 ELSE 0 END) AS dpdgo_enviados,
               SUM(CASE WHEN FLAGDPDGO IS NULL THEN 1 ELSE 0 END) AS dpdgo_pendentes,
               SUM(CASE WHEN HHPCONFIRM = 'Y' THEN 1 ELSE 0 END) AS portal_enviados,
               SUM(CASE WHEN HHPCONFIRM = 'N' THEN 1 ELSE 0 END) AS portal_pendentes,
               SUM(CASE WHEN HHPCONFIRM = 'E' THEN 1 ELSE 0 END) AS portal_erro
        FROM CHRONO_WEB.CW_PT_AS400_NEW_PORTAL
        WHERE HHPDATINC BETWEEN TO_NUMBER(TO_CHAR(SYSDATE - 1,'YYYYMMDD'))
                            AND TO_NUMBER(TO_CHAR(SYSDATE,'YYYYMMDD'))
        GROUP BY HHPDATINC
        ORDER BY HHPDATINC DESC
        """;

    private readonly ApplicationDbContext _db;

    public TvTracingFonte(ApplicationDbContext db) => _db = db;

    public async Task<object> ObterAsync(CancellationToken ct)
    {
        var ligacao = _db.Database.GetDbConnection();
        if (ligacao.State != ConnectionState.Open) await ligacao.OpenAsync(ct);

        using var cmd = ligacao.CreateCommand();
        cmd.CommandText = Sql;
        cmd.CommandTimeout = 180;

        var hoje = int.Parse(DateTime.Today.ToString("yyyyMMdd"));
        var ontem = int.Parse(DateTime.Today.AddDays(-1).ToString("yyyyMMdd"));

        var dias = new List<TvTracingDiaDto>();
        using var leitor = await cmd.ExecuteReaderAsync(ct);

        while (await leitor.ReadAsync(ct))
        {
            var dia = Num(leitor, 0);
            var total = Num(leitor, 1);
            var dpdgoEnviados = Num(leitor, 2);
            var dpdgoPendentes = Num(leitor, 3);
            var portalEnviados = Num(leitor, 4);
            var portalPendentes = Num(leitor, 5);
            var portalErro = Num(leitor, 6);

            dias.Add(new TvTracingDiaDto
            {
                Dia = dia,
                Rotulo = dia == hoje ? "Hoje" : dia == ontem ? "Ontem" : Formatar(dia),
                Total = total,
                DpdGoEnviados = dpdgoEnviados,
                DpdGoPendentes = dpdgoPendentes,
                DpdGoTaxa = total == 0 ? 100 : (int)Math.Round(100.0 * dpdgoEnviados / total),
                PortalEnviados = portalEnviados,
                PortalPendentes = portalPendentes,
                PortalErro = portalErro,
                PortalTaxa = total == 0 ? 100 : (int)Math.Round(100.0 * portalEnviados / total)
            });
        }

        return new TvTracingDto { Dias = dias };
    }

    /// <summary>YYYYMMDD para dd/MM, para os dias que não são hoje nem ontem.</summary>
    private static string Formatar(int yyyymmdd) =>
        $"{yyyymmdd % 100:00}/{yyyymmdd / 100 % 100:00}";

    private static int Num(IDataRecord r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
}
