using System.Data;
using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services.Tv;

/// <summary>
/// Envios SHPNOT da GROUPSHPNOT.GEODT01SPN, hoje e ontem lado a lado.
///
/// A tabela tem 49 milhões de linhas e 50 GB, e não está mapeada no DbContext —
/// daí SQL direto em vez de EF. Tudo sai de **uma só passagem** sobre a janela de
/// dois dias (~100 mil linhas): a query varre uma vez e deriva todos os números
/// em ramos CASE, em vez de repetir a varredura por métrica.
///
/// As datas vêm de SYSDATE, a hora do servidor de base de dados — que é a que
/// carimba as linhas. Usar a hora da máquina da API faria "hoje" saltar sempre
/// que as duas divergissem.
/// </summary>
public class TvShpnotFonte : ITvFonte
{
    public string Chave => "shpnot";

    private const string Sql = """
        WITH base AS (
          SELECT TRUNC(DATAHORA_INSERT) AS dia,
                 FLAGENV, DATAHORAENV, DATAHORA_INSERT,
                 SPTDATTIMX, SCOUNTRYCX, RCOUNTRYCX, MPSIDX
          FROM GROUPSHPNOT.GEODT01SPN
          WHERE DATAHORA_INSERT >= TRUNC(SYSDATE) - 1
        ),
        por_mpsidx AS (
          SELECT dia, MPSIDX, COUNT(*) AS n FROM base GROUP BY dia, MPSIDX
        ),
        dup AS (
          SELECT dia,
                 SUM(n) - COUNT(*) AS duplicados,
                 SUM(CASE WHEN n > 1 THEN 1 ELSE 0 END) AS repetidos,
                 COUNT(*) AS unicos
          FROM por_mpsidx GROUP BY dia
        ),
        -- Nacional/internacional conta cada MPSIDX uma única vez: um envio
        -- duplicado na tabela não são dois envios no mundo real.
        primeira_de_cada AS (
          SELECT dia, SCOUNTRYCX, RCOUNTRYCX FROM (
            SELECT dia, SCOUNTRYCX, RCOUNTRYCX,
                   ROW_NUMBER() OVER (PARTITION BY dia, MPSIDX ORDER BY DATAHORA_INSERT) AS rn
            FROM base
          ) WHERE rn = 1
        ),
        geo AS (
          SELECT dia,
                 SUM(CASE WHEN SCOUNTRYCX = RCOUNTRYCX THEN 1 ELSE 0 END) AS nacional,
                 SUM(CASE WHEN SCOUNTRYCX <> RCOUNTRYCX THEN 1 ELSE 0 END) AS internacional
          FROM primeira_de_cada GROUP BY dia
        )
        SELECT b.dia,
          COUNT(*) AS total,
          SUM(CASE WHEN b.FLAGENV = 'Y' THEN 1 ELSE 0 END) AS sucesso,
          SUM(CASE WHEN b.FLAGENV = 'N' THEN 1 ELSE 0 END) AS erro,
          SUM(CASE WHEN b.FLAGENV IS NULL OR b.FLAGENV NOT IN ('Y','N') THEN 1 ELSE 0 END) AS outros,
          SUM(CASE WHEN TRUNC(b.DATAHORAENV) = b.dia THEN 1 ELSE 0 END) AS env_proprio_dia,
          SUM(CASE WHEN TRUNC(b.DATAHORAENV) <> b.dia THEN 1 ELSE 0 END) AS env_outro_dia,
          SUM(CASE WHEN b.DATAHORAENV IS NULL THEN 1 ELSE 0 END) AS env_sem_data,
          SUM(CASE WHEN REGEXP_LIKE(b.SPTDATTIMX,'^[0-9]{14}$')
                    AND SUBSTR(b.SPTDATTIMX,1,8) = TO_CHAR(b.dia,'YYYYMMDD') THEN 1 ELSE 0 END) AS spt_dentro,
          SUM(CASE WHEN REGEXP_LIKE(b.SPTDATTIMX,'^[0-9]{14}$')
                    AND SUBSTR(b.SPTDATTIMX,1,8) <> TO_CHAR(b.dia,'YYYYMMDD') THEN 1 ELSE 0 END) AS spt_fora,
          SUM(CASE WHEN b.SPTDATTIMX IS NULL
                    OR NOT REGEXP_LIKE(b.SPTDATTIMX,'^[0-9]{14}$') THEN 1 ELSE 0 END) AS spt_invalido,
          MAX(d.duplicados) AS duplicados,
          MAX(d.repetidos) AS repetidos,
          MAX(d.unicos) AS unicos,
          MAX(g.nacional) AS nacional,
          MAX(g.internacional) AS internacional
        FROM base b
        JOIN dup d ON d.dia = b.dia
        JOIN geo g ON g.dia = b.dia
        GROUP BY b.dia
        ORDER BY b.dia DESC
        """;

    private readonly ApplicationDbContext _db;

    public TvShpnotFonte(ApplicationDbContext db) => _db = db;

    public async Task<object> ObterAsync(CancellationToken ct)
    {
        var ligacao = _db.Database.GetDbConnection();
        if (ligacao.State != ConnectionState.Open) await ligacao.OpenAsync(ct);

        using var cmd = ligacao.CreateCommand();
        cmd.CommandText = Sql;
        cmd.CommandTimeout = 120;

        var dias = new List<TvShpnotDiaDto>();
        using var leitor = await cmd.ExecuteReaderAsync(ct);

        var hoje = DateTime.Today;

        while (await leitor.ReadAsync(ct))
        {
            var dia = leitor.GetDateTime(0);

            dias.Add(new TvShpnotDiaDto
            {
                Dia = dia,
                Rotulo = dia.Date == hoje ? "Hoje" : dia.Date == hoje.AddDays(-1) ? "Ontem" : dia.ToString("dd/MM"),
                Total = Num(leitor, 1),
                Sucesso = Num(leitor, 2),
                Erro = Num(leitor, 3),
                OutrosEstados = Num(leitor, 4),
                EnviadosNoProprioDia = Num(leitor, 5),
                EnviadosNoutroDia = Num(leitor, 6),
                SemDataEnvio = Num(leitor, 7),
                SptDentroDoDia = Num(leitor, 8),
                SptForaDoDia = Num(leitor, 9),
                SptFormatoInvalido = Num(leitor, 10),
                Duplicados = Num(leitor, 11),
                MpsidxRepetidos = Num(leitor, 12),
                Unicos = Num(leitor, 13),
                Nacional = Num(leitor, 14),
                Internacional = Num(leitor, 15)
            });
        }

        return new TvShpnotDto { Dias = dias };
    }

    /// <summary>O Oracle devolve NUMBER como decimal; as contagens são sempre inteiras.</summary>
    private static int Num(IDataRecord r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
}
