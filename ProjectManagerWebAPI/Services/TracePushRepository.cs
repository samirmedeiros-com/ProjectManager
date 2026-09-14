using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using ProjectManagerWebAPI.Models.TracePush;
using ProjectManagerWebAPI.Models.Contas;

namespace ProjectManagerWebAPI.Services;

public class TracePushException(string message) : Exception(message);

public interface ITracePushRepository
{
    Task<PushEstatisticas> EstatisticasAsync(DateTime dia, CancellationToken ct);
    Task<List<PushPorUserLogin>> PorUserLoginAsync(DateTime dia, CancellationToken ct);
    Task<List<PushPorHora>> PorHoraAsync(DateTime dia, string userLogin, CancellationToken ct);
    Task<Pagina<PushResumo>> ProcurarAsync(FiltroPush filtro, CancellationToken ct);
    Task<PushDetalhe?> ObterAsync(string hhpRowId, CancellationToken ct);
    Task<List<PushResumo>> ObterVariosAsync(IReadOnlyCollection<string> hhpRowIds, CancellationToken ct);
    Task<int> ReporPendenteAsync(IReadOnlyCollection<string> hhpRowIds, CancellationToken ct);
}

/// <summary>Os filtros da pesquisa, tal como chegam do ecrã.</summary>
public sealed record FiltroPush(
    DateTime? Dia,
    string? UserLogin,
    string? Conta,
    string? Guia,
    string? Estado,
    int Pagina,
    int Tamanho);

/// <summary>
/// Leitura e reenvio de CHRONO_WEB.CW_TRACEPUSH — a fila de envio de eventos de trace para os
/// webservices dos clientes.
///
/// <para><b>A data da pesquisa é DATAINSER e não DATAFLAG.</b> DATAFLAG é o instante em que a
/// linha foi processada, por isso está a <b>NULL em tudo o que está pendente</b>: filtrar por
/// DATAFLAG faria desaparecer do ecrã exactamente as linhas que interessa ver. DATAINSER está
/// sempre preenchida e tem índice próprio (CW_TRACE_DATA).</para>
///
/// <para><b>Nunca consultar sem data.</b> A tabela tem ~64 milhões de linhas e 62 GB; um
/// predicado só por DESTINATARIO não chega — essa coluna tem apenas ~620 valores distintos e
/// uma conta sozinha apanha mais de um milhão de linhas. A excepção é o número de guia, que
/// é praticamente único e tem índice (CW_TRACEPUSH_ID): aí a data não faz falta e seria um
/// estorvo, porque quem procura uma guia raramente sabe o dia em que ela foi despachada.</para>
///
/// <para>O reenvio <b>não chama o webservice do cliente</b>: repõe FLAG='N' e limpa DATAFLAG,
/// devolvendo a linha à fila. É o mesmo contrato que a Gestão de Dados tem com as contas —
/// quem envia é sempre o processo automático, e assim não há aqui uma segunda implementação
/// do formato de cada cliente (SOAP, REST, autenticação) a divergir da primeira.</para>
/// </summary>
public class TracePushRepository : ITracePushRepository
{
    private const string Tabela = "CHRONO_WEB.CW_TRACEPUSH";

    private readonly string _connectionString;
    private readonly int _timeout;
    private readonly ILogger<TracePushRepository> _logger;

    public TracePushRepository(IConfiguration configuration, IOptions<ContasOptions> opcoes, ILogger<TracePushRepository> logger)
    {
        _logger = logger;
        _timeout = opcoes.Value.TimeoutSegundos;
        // A mesma ligação da Gestão de Dados: o utilizador "Wsdpd" do appsettings é o CHRONO_WEB.
        _connectionString = configuration.GetConnectionString(opcoes.Value.ConnectionStringNome)
            ?? throw new InvalidOperationException($"Falta a ligação '{opcoes.Value.ConnectionStringNome}' em ConnectionStrings.");
    }

    private async Task<OracleConnection> AbrirAsync(CancellationToken ct)
    {
        var ligacao = new OracleConnection(_connectionString);
        await ligacao.OpenAsync(ct);
        return ligacao;
    }

    private OracleCommand Comando(OracleConnection ligacao, string sql)
    {
        var cmd = new OracleCommand(sql, ligacao) { BindByName = true };
        cmd.CommandTimeout = _timeout;
        return cmd;
    }

    // ------------------------------------------------------------ agregados

    /// <summary>
    /// Os três buckets numa passagem só. O CASE reproduz o mapa de letras: tudo o que não é
    /// Y nem N conta como erro, e por isso os cartões somam sempre o total de linhas do dia.
    /// </summary>
    private const string SqlBuckets = @"
  SUM(CASE WHEN NVL(FLAG,'N') = 'Y' THEN 1 ELSE 0 END) AS ENVIADOS,
  SUM(CASE WHEN NVL(FLAG,'N') = 'N' THEN 1 ELSE 0 END) AS PENDENTES,
  SUM(CASE WHEN NVL(FLAG,'N') NOT IN ('Y','N') THEN 1 ELSE 0 END) AS ERROS,
  COUNT(*) AS TOTAL";

    public async Task<PushEstatisticas> EstatisticasAsync(DateTime dia, CancellationToken ct)
    {
        var sql = $"SELECT {SqlBuckets} FROM {Tabela} WHERE DATAINSER >= :de AND DATAINSER < :ate";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = Comando(ligacao, sql);
        LigarDia(cmd, dia);

        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        var e = new PushEstatisticas { Dia = dia.Date };
        if (await leitor.ReadAsync(ct))
        {
            e.Enviados = Inteiro(leitor, 0);
            e.Pendentes = Inteiro(leitor, 1);
            e.Erros = Inteiro(leitor, 2);
            e.Total = Inteiro(leitor, 3);
        }
        return e;
    }

    public async Task<List<PushPorUserLogin>> PorUserLoginAsync(DateTime dia, CancellationToken ct)
    {
        var sql = $@"
SELECT NVL(USERLOGIN,'(sem identificação)') AS LOGIN, {SqlBuckets}
FROM {Tabela}
WHERE DATAINSER >= :de AND DATAINSER < :ate
GROUP BY NVL(USERLOGIN,'(sem identificação)')
ORDER BY TOTAL DESC";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = Comando(ligacao, sql);
        LigarDia(cmd, dia);

        var linhas = new List<PushPorUserLogin>();
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
        {
            linhas.Add(new PushPorUserLogin
            {
                UserLogin = leitor.GetString(0).Trim(),
                Enviados = Inteiro(leitor, 1),
                Pendentes = Inteiro(leitor, 2),
                Erros = Inteiro(leitor, 3),
                Total = Inteiro(leitor, 4),
            });
        }
        return linhas;
    }

    /// <summary>
    /// As 24 barras do gráfico. As horas sem tráfego não vêm do GROUP BY — são preenchidas a
    /// zero aqui, senão o gráfico encolhia e uma paragem de seis horas deixava de se ver.
    /// </summary>
    public async Task<List<PushPorHora>> PorHoraAsync(DateTime dia, string userLogin, CancellationToken ct)
    {
        var sql = $@"
SELECT TO_NUMBER(TO_CHAR(DATAINSER,'HH24')) AS HORA, {SqlBuckets}
FROM {Tabela}
WHERE DATAINSER >= :de AND DATAINSER < :ate AND USERLOGIN = :userlogin
GROUP BY TO_NUMBER(TO_CHAR(DATAINSER,'HH24'))";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = Comando(ligacao, sql);
        LigarDia(cmd, dia);
        cmd.Parameters.Add(new OracleParameter("userlogin", OracleDbType.Varchar2) { Value = userLogin });

        var porHora = Enumerable.Range(0, 24).Select(h => new PushPorHora { Hora = h }).ToList();
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
        {
            var hora = Inteiro(leitor, 0);
            if (hora is < 0 or > 23) continue;
            porHora[hora].Enviados = Inteiro(leitor, 1);
            porHora[hora].Pendentes = Inteiro(leitor, 2);
            porHora[hora].Erros = Inteiro(leitor, 3);
            porHora[hora].Total = Inteiro(leitor, 4);
        }
        return porHora;
    }

    // ------------------------------------------------------------- pesquisa

    private const string ColunasResumo = @"
  t.HHPROWID, t.ID, t.NUMERO, t.USERLOGIN, t.DESTINATARIO, t.REFCLI,
  t.CODIGO, t.DESCPT, t.FLAG, t.DATAINSER, t.DATAFLAG, t.PURL";

    public async Task<Pagina<PushResumo>> ProcurarAsync(FiltroPush filtro, CancellationToken ct)
    {
        var pagina = filtro.Pagina < 1 ? 1 : filtro.Pagina;
        var tamanho = filtro.Tamanho is < 1 or > 200 ? 10 : filtro.Tamanho;
        var lo = (pagina - 1) * tamanho;
        var hi = lo + tamanho;

        var (where, parametros) = ConstruirWhere(filtro);

        // Três níveis, como na listagem de contas: o interior filtra e ordena e traz o total do
        // conjunto filtrado com COUNT(*) OVER(); os de fora cortam a fatia por ROWNUM. O COUNT
        // tem de ficar aqui dentro — uma segunda consulta a contar percorreria a tabela outra vez.
        var sql = $@"
SELECT * FROM (
  SELECT a.*, ROWNUM AS RN FROM (
    SELECT {ColunasResumo}, COUNT(*) OVER() AS TOTAL_FILTRADO
    FROM {Tabela} t
    WHERE {where}
    ORDER BY t.DATAINSER DESC, t.HHPROWID DESC
  ) a WHERE ROWNUM <= :hi
) WHERE RN > :lo";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = Comando(ligacao, sql);
        foreach (var p in parametros) cmd.Parameters.Add(p);
        cmd.Parameters.Add(new OracleParameter("hi", OracleDbType.Int32) { Value = hi });
        cmd.Parameters.Add(new OracleParameter("lo", OracleDbType.Int32) { Value = lo });

        var resultado = new Pagina<PushResumo> { PaginaAtual = pagina, Tamanho = tamanho };
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
        {
            resultado.Itens.Add(LerResumo(leitor));
            resultado.Total = Inteiro(leitor, leitor.GetOrdinal("TOTAL_FILTRADO"));
        }
        return resultado;
    }

    /// <summary>
    /// O WHERE da pesquisa. Duas formas, e só duas:
    /// <list type="bullet">
    /// <item>com número de guia: procura só por ele, em toda a tabela (coluna ID, indexada);</item>
    /// <item>sem número de guia: exige sempre um dia, e os restantes filtros apertam-no.</item>
    /// </list>
    /// Não há uma terceira em que nem guia nem data vêm preenchidas — seria um varrimento de
    /// 62 GB a pedido de quem carregou em Procurar sem escrever nada.
    /// </summary>
    private static (string Where, List<OracleParameter> Parametros) ConstruirWhere(FiltroPush filtro)
    {
        var parametros = new List<OracleParameter>();
        var guia = filtro.Guia?.Trim();

        if (!string.IsNullOrWhiteSpace(guia))
        {
            parametros.Add(new OracleParameter("guia", OracleDbType.Varchar2) { Value = guia.ToUpperInvariant() });
            return ("t.ID = :guia", parametros);
        }

        if (filtro.Dia is null)
            throw new TracePushException("Indique uma data, ou então um número de guia.");

        var condicoes = new List<string> { "t.DATAINSER >= :de AND t.DATAINSER < :ate" };
        parametros.Add(new OracleParameter("de", OracleDbType.Date) { Value = filtro.Dia.Value.Date });
        parametros.Add(new OracleParameter("ate", OracleDbType.Date) { Value = filtro.Dia.Value.Date.AddDays(1) });

        if (!string.IsNullOrWhiteSpace(filtro.UserLogin))
        {
            condicoes.Add("t.USERLOGIN = :userlogin");
            parametros.Add(new OracleParameter("userlogin", OracleDbType.Varchar2) { Value = filtro.UserLogin.Trim() });
        }

        if (!string.IsNullOrWhiteSpace(filtro.Conta))
        {
            // Prefixo: as contas escrevem-se ora com ora sem os zeros da frente do segmento seguinte.
            condicoes.Add("t.DESTINATARIO LIKE :conta");
            parametros.Add(new OracleParameter("conta", OracleDbType.Varchar2) { Value = filtro.Conta.Trim() + "%" });
        }

        var estado = EstadoPushMapa.FiltroSql(filtro.Estado);
        if (estado is not null) condicoes.Add(estado);

        return (string.Join(" AND ", condicoes), parametros);
    }

    // -------------------------------------------------------------- detalhe

    public async Task<PushDetalhe?> ObterAsync(string hhpRowId, CancellationToken ct)
    {
        // REQUESTLOB é o pedido quando não coube no VARCHAR2(4000) do REQUEST; quando existe,
        // é ele que vale. Só se lê aqui, no detalhe — na lista custaria um CLOB por linha.
        var sql = $@"
SELECT {ColunasResumo}, t.REQUEST, t.RESPONSE, t.REQUESTLOB, t.PUSER,
       t.DATA, t.DATA_EXT, t.OBS, t.HHPDRIVER, t.PUDO_ID
FROM {Tabela} t
WHERE t.HHPROWID = :id";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = Comando(ligacao, sql);
        cmd.Parameters.Add(new OracleParameter("id", OracleDbType.Varchar2) { Value = hhpRowId });

        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        if (!await leitor.ReadAsync(ct)) return null;

        var resumo = LerResumo(leitor);
        var pedidoCurto = Texto(leitor, "REQUEST");
        var pedidoLongo = await LerClobAsync(leitor, "REQUESTLOB", ct);

        return new PushDetalhe
        {
            HhpRowId = resumo.HhpRowId,
            Guia = resumo.Guia,
            Lote = resumo.Lote,
            UserLogin = resumo.UserLogin,
            Conta = resumo.Conta,
            Referencia = resumo.Referencia,
            Codigo = resumo.Codigo,
            Descricao = resumo.Descricao,
            Flag = resumo.Flag,
            Estado = resumo.Estado,
            Criado = resumo.Criado,
            Processado = resumo.Processado,
            Destino = resumo.Destino,
            Pedido = string.IsNullOrWhiteSpace(pedidoLongo) ? pedidoCurto : pedidoLongo,
            Resposta = Texto(leitor, "RESPONSE"),
            Utilizador = Texto(leitor, "PUSER"),
            DataEvento = Texto(leitor, "DATA_EXT") ?? Texto(leitor, "DATA"),
            Observacoes = Texto(leitor, "OBS"),
            Motorista = Texto(leitor, "HHPDRIVER"),
            Pudo = Texto(leitor, "PUDO_ID"),
        };
    }

    public async Task<List<PushResumo>> ObterVariosAsync(IReadOnlyCollection<string> hhpRowIds, CancellationToken ct)
    {
        if (hhpRowIds.Count == 0) return [];

        var nomes = hhpRowIds.Select((_, i) => $":id{i}").ToArray();
        var sql = $"SELECT {ColunasResumo} FROM {Tabela} t WHERE t.HHPROWID IN ({string.Join(",", nomes)})";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = Comando(ligacao, sql);
        var i = 0;
        foreach (var id in hhpRowIds)
            cmd.Parameters.Add(new OracleParameter($"id{i++}", OracleDbType.Varchar2) { Value = id });

        var linhas = new List<PushResumo>();
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct)) linhas.Add(LerResumo(leitor));
        return linhas;
    }

    // -------------------------------------------------------------- reenvio

    /// <summary>
    /// Devolve as linhas à fila: FLAG='N' e DATAFLAG a NULL, que é exactamente o estado em que
    /// uma linha nasce. Limpar DATAFLAG não é cosmético — é o que faz a linha voltar a contar
    /// como pendente e o que impede que fique com a data do envio falhado colada para sempre.
    /// </summary>
    public async Task<int> ReporPendenteAsync(IReadOnlyCollection<string> hhpRowIds, CancellationToken ct)
    {
        if (hhpRowIds.Count == 0) return 0;

        var nomes = hhpRowIds.Select((_, i) => $":id{i}").ToArray();
        var sql = $@"
UPDATE {Tabela}
SET FLAG = 'N', DATAFLAG = NULL
WHERE HHPROWID IN ({string.Join(",", nomes)})";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = Comando(ligacao, sql);
        var i = 0;
        foreach (var id in hhpRowIds)
            cmd.Parameters.Add(new OracleParameter($"id{i++}", OracleDbType.Varchar2) { Value = id });

        var afetadas = await cmd.ExecuteNonQueryAsync(ct);
        _logger.LogInformation("Reenvio: {Afetadas} de {Pedidas} linhas repostas a pendente", afetadas, hhpRowIds.Count);
        return afetadas;
    }

    // --------------------------------------------------------------- leitura

    private static PushResumo LerResumo(OracleDataReader leitor)
    {
        var flag = Texto(leitor, "FLAG");
        return new PushResumo
        {
            HhpRowId = Texto(leitor, "HHPROWID") ?? "",
            Guia = Texto(leitor, "ID"),
            Lote = Texto(leitor, "NUMERO"),
            UserLogin = Texto(leitor, "USERLOGIN"),
            Conta = Texto(leitor, "DESTINATARIO"),
            Referencia = Texto(leitor, "REFCLI"),
            Codigo = Texto(leitor, "CODIGO"),
            Descricao = Texto(leitor, "DESCPT"),
            Flag = flag,
            Estado = EstadoPushMapa.DaFlag(flag).ToString().ToLowerInvariant(),
            Criado = Data(leitor, "DATAINSER"),
            Processado = Data(leitor, "DATAFLAG"),
            Destino = Texto(leitor, "PURL"),
        };
    }

    /// <summary>Texto sem os brancos à direita: há colunas de origem AS400 que vêm cheias deles.</summary>
    private static string? Texto(OracleDataReader leitor, string coluna)
    {
        var i = leitor.GetOrdinal(coluna);
        if (leitor.IsDBNull(i)) return null;
        var valor = leitor.GetString(i).Trim();
        return valor.Length == 0 ? null : valor;
    }

    private static DateTime? Data(OracleDataReader leitor, string coluna)
    {
        var i = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(i) ? null : leitor.GetDateTime(i);
    }

    private static int Inteiro(OracleDataReader leitor, int i) => leitor.IsDBNull(i) ? 0 : Convert.ToInt32(leitor.GetValue(i));

    private static async Task<string?> LerClobAsync(OracleDataReader leitor, string coluna, CancellationToken ct)
    {
        var i = leitor.GetOrdinal(coluna);
        if (leitor.IsDBNull(i)) return null;
        await using var clob = leitor.GetOracleClob(i);
        var texto = clob.Value?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto;
    }

    private static void LigarDia(OracleCommand cmd, DateTime dia)
    {
        cmd.Parameters.Add(new OracleParameter("de", OracleDbType.Date) { Value = dia.Date });
        cmd.Parameters.Add(new OracleParameter("ate", OracleDbType.Date) { Value = dia.Date.AddDays(1) });
    }
}
