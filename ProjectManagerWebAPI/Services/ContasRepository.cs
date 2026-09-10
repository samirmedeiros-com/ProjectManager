using System.Reflection;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using ProjectManagerWebAPI.Models.Contas;

namespace ProjectManagerWebAPI.Services;

public class ContasException(string message) : Exception(message);

public interface IContasRepository
{
    Task<ContasEstatisticas> EstatisticasAsync(CancellationToken ct);
    Task<Pagina<ContaResumo>> ListarContasAsync(string? procura, string? flagportal, int pagina, int tamanho, CancellationToken ct);
    Task<Conta?> ObterContaAsync(string eeent, CancellationToken ct);
    Task<Conta> GravarContaAsync(Conta conta, CancellationToken ct);
    Task<List<SubConta>> ListarSubContasAsync(string eeent, CancellationToken ct);
    Task<SubConta?> ObterSubContaAsync(string eeent, string emend, CancellationToken ct);
    Task<SubConta> GravarSubContaAsync(SubConta subConta, CancellationToken ct);
    Task<List<SubContaEnvio>> ObterParaEnvioAsync(string eeent, string? emend, CancellationToken ct);
    Task MarcarFlagContaAsync(string eeent, string flag, CancellationToken ct);
    Task MarcarFlagSubContaAsync(string eeent, string emend, string flag, CancellationToken ct);
}

/// <summary>
/// Acesso às tabelas WSDPD.AS400_CONTAS e WSDPD.AS400_SUBCONTAS.
///
/// As tabelas não são desta aplicação — são alimentadas pelo AS400 e lidas pelo processo
/// automático que envia ao portal. Este repositório escreve nelas com o mesmo contrato:
/// gravar repõe FLAGPORTAL='N' (por enviar), e é o envio que põe 'Y' ou 'E'. Assim uma
/// alteração feita aqui sai na mesma pelo processo automático, mesmo que ninguém carregue
/// no botão de enviar.
/// </summary>
public class ContasRepository : IContasRepository
{
    private readonly string _connectionString;
    private readonly ContasOptions _opcoes;

    private static readonly PropriedadeColuna[] ColunasConta = Mapear<Conta>();
    private static readonly PropriedadeColuna[] ColunasSubConta = Mapear<SubConta>();

    private sealed record PropriedadeColuna(PropertyInfo Propriedade, string Coluna, bool Chave);

    public ContasRepository(IConfiguration configuration, IOptions<ContasOptions> opcoes)
    {
        _opcoes = opcoes.Value;
        _connectionString = configuration.GetConnectionString(_opcoes.ConnectionStringNome)
            ?? throw new InvalidOperationException(
                $"Falta a ligação '{_opcoes.ConnectionStringNome}' em ConnectionStrings.");
    }

    private static PropriedadeColuna[] Mapear<T>() =>
        [.. typeof(T).GetProperties()
            .Select(p => new { p, a = p.GetCustomAttribute<ColunaAttribute>() })
            .Where(x => x.a is not null)
            .Select(x => new PropriedadeColuna(x.p, x.a!.Nome, x.a.Chave))];

    private async Task<OracleConnection> AbrirAsync(CancellationToken ct)
    {
        var ligacao = new OracleConnection(_connectionString);
        await ligacao.OpenAsync(ct);
        return ligacao;
    }

    // ----------------------------------------------------------- estatísticas

    public async Task<ContasEstatisticas> EstatisticasAsync(CancellationToken ct)
    {
        await using var ligacao = await AbrirAsync(ct);

        return new ContasEstatisticas
        {
            Contas = await ContarPorFlagAsync(ligacao, "WSDPD.AS400_CONTAS", ct),
            SubContas = await ContarPorFlagAsync(ligacao, "WSDPD.AS400_SUBCONTAS", ct),
        };
    }

    /// <summary>
    /// Uma passagem só por tabela, agrupada por FLAGPORTAL. Os buckets nomeados (Y/N/E) e um
    /// "outros" para qualquer valor inesperado — assim o total do dashboard bate sempre certo
    /// com o número de linhas, mesmo que apareça uma flag que não a que conhecemos.
    /// </summary>
    private async Task<EstatisticaTabela> ContarPorFlagAsync(OracleConnection ligacao, string tabela, CancellationToken ct)
    {
        var sql = $"SELECT NVL(FLAGPORTAL, ' ') AS FLAG, COUNT(*) AS QTD FROM {tabela} GROUP BY NVL(FLAGPORTAL, ' ')";

        await using var cmd = new OracleCommand(sql, ligacao) { BindByName = true };
        cmd.CommandTimeout = _opcoes.TimeoutSegundos;

        var estatistica = new EstatisticaTabela();
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
        {
            var flag = leitor.GetString(0).Trim();
            var qtd = Convert.ToInt32(leitor.GetValue(1));
            estatistica.Total += qtd;

            switch (flag)
            {
                case "Y": estatistica.Transmitidas = qtd; break;
                case "N": estatistica.ATransmitir = qtd; break;
                case "E": estatistica.Erros = qtd; break;
                default: estatistica.Outros += qtd; break;
            }
        }

        return estatistica;
    }

    // ---------------------------------------------------------------- contas

    public async Task<Pagina<ContaResumo>> ListarContasAsync(string? procura, string? flagportal, int pagina, int tamanho, CancellationToken ct)
    {
        pagina = pagina < 1 ? 1 : pagina;
        tamanho = NormalizarTamanho(tamanho);
        var lo = (pagina - 1) * tamanho;
        var hi = lo + tamanho;

        // Três níveis:
        //  - o interior filtra e ordena, e o COUNT(*) OVER() traz o total do conjunto filtrado
        //    numa passagem só (é preciso para saber quantas páginas há);
        //  - o do meio numera com ROWNUM para cortar a página;
        //  - o de fora conta as subcontas por conta — subconsulta escalar, e só para as linhas
        //    da página, não para a tabela toda (com join, uma conta sem subcontas sumia da lista).
        var sql = @"SELECT p.IDT, p.EEENT, p.EENOM, p.FTSIT, p.FLAGPORTAL, p.TOTAL_GERAL,
                           (SELECT COUNT(*) FROM WSDPD.AS400_SUBCONTAS s WHERE s.EMENT = p.EEENT) AS SUBCONTAS
                      FROM (SELECT q.*, ROWNUM rn FROM (
                              SELECT c.IDT, c.EEENT, c.EENOM, c.FTSIT, c.FLAGPORTAL,
                                     COUNT(*) OVER() AS TOTAL_GERAL
                                FROM WSDPD.AS400_CONTAS c
                               WHERE (:procura IS NULL
                                      OR c.EEENT LIKE :procura_like
                                      OR UPPER(c.EENOM) LIKE UPPER(:procura_like))
                                 AND (:flag IS NULL OR c.FLAGPORTAL = :flag)
                               ORDER BY c.EEENT
                            ) q WHERE ROWNUM <= :hi) p
                     WHERE p.rn > :lo";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = new OracleCommand(sql, ligacao) { BindByName = true };
        cmd.CommandTimeout = _opcoes.TimeoutSegundos;

        var texto = string.IsNullOrWhiteSpace(procura) ? null : procura.Trim();
        cmd.Parameters.Add(":procura", (object?)texto ?? DBNull.Value);
        cmd.Parameters.Add(":procura_like", (object?)(texto is null ? null : $"%{texto}%") ?? DBNull.Value);
        cmd.Parameters.Add(":flag", (object?)(string.IsNullOrWhiteSpace(flagportal) ? null : flagportal.Trim()) ?? DBNull.Value);
        cmd.Parameters.Add(":hi", hi);
        cmd.Parameters.Add(":lo", lo);

        var resultado = new Pagina<ContaResumo> { PaginaAtual = pagina, Tamanho = tamanho };
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
        {
            resultado.Total = (int)(Decimal(leitor, "TOTAL_GERAL") ?? 0m);
            resultado.Itens.Add(new ContaResumo
            {
                Idt = Decimal(leitor, "IDT"),
                Eeent = Texto(leitor, "EEENT"),
                Eenom = Texto(leitor, "EENOM"),
                Ftsit = Texto(leitor, "FTSIT"),
                Flagportal = Texto(leitor, "FLAGPORTAL"),
                SubContas = (int)(Decimal(leitor, "SUBCONTAS") ?? 0m)
            });
        }

        return resultado;
    }

    /// <summary>Só 10, 50 ou 100 por página — os tamanhos que o ecrã oferece.</summary>
    private static int NormalizarTamanho(int tamanho) => tamanho switch
    {
        10 or 50 or 100 => tamanho,
        _ => 10
    };

    public async Task<Conta?> ObterContaAsync(string eeent, CancellationToken ct)
    {
        var sql = $"SELECT {Colunas(ColunasConta)} FROM WSDPD.AS400_CONTAS WHERE EEENT = :eeent";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = new OracleCommand(sql, ligacao) { BindByName = true };
        cmd.CommandTimeout = _opcoes.TimeoutSegundos;
        cmd.Parameters.Add(":eeent", eeent);

        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        if (!await leitor.ReadAsync(ct)) return null;

        return Ler<Conta>(leitor, ColunasConta);
    }

    public async Task<Conta> GravarContaAsync(Conta conta, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(conta.Eeent))
            throw new ContasException("O número da conta (EEENT) é obrigatório.");

        conta.Eeent = conta.Eeent.Trim();
        // Qualquer alteração volta à fila de envio, tenha ou não sido enviada antes.
        conta.Flagportal = "N";

        var existente = await ObterContaAsync(conta.Eeent, ct);

        await using var ligacao = await AbrirAsync(ct);

        if (existente is null)
        {
            await InserirAsync(ligacao, "WSDPD.AS400_CONTAS", ColunasConta, conta, ct);
        }
        else
        {
            conta.Idt = existente.Idt;
            await AtualizarAsync(ligacao, "WSDPD.AS400_CONTAS", ColunasConta, conta,
                "EEENT = :chave_eeent", [new OracleParameter(":chave_eeent", conta.Eeent)], ct);
        }

        return await ObterContaAsync(conta.Eeent, ct)
            ?? throw new ContasException("A conta foi gravada mas não voltou a ser encontrada.");
    }

    public async Task MarcarFlagContaAsync(string eeent, string flag, CancellationToken ct)
    {
        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = new OracleCommand(
            "UPDATE WSDPD.AS400_CONTAS SET FLAGPORTAL = :flag WHERE EEENT = :eeent", ligacao)
        { BindByName = true, CommandTimeout = _opcoes.TimeoutSegundos };
        cmd.Parameters.Add(":flag", flag);
        cmd.Parameters.Add(":eeent", eeent);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ------------------------------------------------------------- subcontas

    public async Task<List<SubConta>> ListarSubContasAsync(string eeent, CancellationToken ct)
    {
        var sql = $"SELECT {Colunas(ColunasSubConta)} FROM WSDPD.AS400_SUBCONTAS WHERE EMENT = :eeent ORDER BY EMEND";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = new OracleCommand(sql, ligacao) { BindByName = true };
        cmd.CommandTimeout = _opcoes.TimeoutSegundos;
        cmd.Parameters.Add(":eeent", eeent);

        var lista = new List<SubConta>();
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
            lista.Add(Ler<SubConta>(leitor, ColunasSubConta));

        return lista;
    }

    public async Task<SubConta?> ObterSubContaAsync(string eeent, string emend, CancellationToken ct)
    {
        var sql = $"SELECT {Colunas(ColunasSubConta)} FROM WSDPD.AS400_SUBCONTAS WHERE EMENT = :eeent AND EMEND = :emend";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = new OracleCommand(sql, ligacao) { BindByName = true };
        cmd.CommandTimeout = _opcoes.TimeoutSegundos;
        cmd.Parameters.Add(":eeent", eeent);
        cmd.Parameters.Add(":emend", emend);

        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        if (!await leitor.ReadAsync(ct)) return null;

        return Ler<SubConta>(leitor, ColunasSubConta);
    }

    public async Task<SubConta> GravarSubContaAsync(SubConta subConta, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subConta.Ement))
            throw new ContasException("O número da conta (EMENT) é obrigatório.");
        if (string.IsNullOrWhiteSpace(subConta.Emend))
            throw new ContasException("O número da subconta (EMEND) é obrigatório.");

        subConta.Ement = subConta.Ement.Trim();
        subConta.Emend = subConta.Emend.Trim();
        subConta.Flagportal = "N";

        if (await ObterContaAsync(subConta.Ement, ct) is null)
            throw new ContasException($"A conta {subConta.Ement} não existe. Crie a conta antes da subconta.");

        var existente = await ObterSubContaAsync(subConta.Ement, subConta.Emend, ct);

        await using var ligacao = await AbrirAsync(ct);

        if (existente is null)
        {
            await InserirAsync(ligacao, "WSDPD.AS400_SUBCONTAS", ColunasSubConta, subConta, ct);
        }
        else
        {
            subConta.Idt = existente.Idt;
            await AtualizarAsync(ligacao, "WSDPD.AS400_SUBCONTAS", ColunasSubConta, subConta,
                "EMENT = :chave_ement AND EMEND = :chave_emend",
                [new OracleParameter(":chave_ement", subConta.Ement), new OracleParameter(":chave_emend", subConta.Emend)],
                ct);
        }

        return await ObterSubContaAsync(subConta.Ement, subConta.Emend, ct)
            ?? throw new ContasException("A subconta foi gravada mas não voltou a ser encontrada.");
    }

    public async Task MarcarFlagSubContaAsync(string eeent, string emend, string flag, CancellationToken ct)
    {
        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = new OracleCommand(
            "UPDATE WSDPD.AS400_SUBCONTAS SET FLAGPORTAL = :flag WHERE EMENT = :eeent AND EMEND = :emend", ligacao)
        { BindByName = true, CommandTimeout = _opcoes.TimeoutSegundos };
        cmd.Parameters.Add(":flag", flag);
        cmd.Parameters.Add(":eeent", eeent);
        cmd.Parameters.Add(":emend", emend);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ----------------------------------------------------------------- envio

    /// <summary>
    /// Lê as subcontas já enriquecidas pelos joins que o processo automático usa. A consulta é
    /// a mesma do ContasPortal_NET de propósito: o portal tem de receber exatamente o mesmo
    /// payload venha ele daqui ou do processo em ciclo.
    /// </summary>
    public async Task<List<SubContaEnvio>> ObterParaEnvioAsync(string eeent, string? emend, CancellationToken ct)
    {
        var sql = @"select sub.IDT, sub.EMENT, sub.EMEND, sub.EMDSC, sub.EMMOR, sub.EMLOC, sub.EMPOS,
                           sub.EMPAI, sub.EMENF, sub.EMTFT, sub.EMEXP, sub.BICACT, sub.EMTRA,
                           sub.TELEFONE, sub.TELEMOVEL, sub.EMAIL,
                           case when sub.SERVICE_RC = f.srv_from then f.srv_to else sub.SERVICE_RC end as SERVICE_RC,
                           sub.SERVICEB2C, sub.SERV_FRIO, sub.BICSCI, sub.BICSNIF, sub.BICSCCC, sub.BICSPRACA,
                           sub.BICSCCC2, sub.BICSUSR2, sub.BICSSERV, sub.BICSPROD, sub.BICVCLI, sub.ACTIVE,
                           sub.GRASSINADA, sub.PREDICT, sub.MULTIPARCELA, sub.COD, sub.SERVICO_RC_COM_COD,
                           sub.TEXTO_SERVICO, sub.TEMPLATE_ETIQUETA, sub.TEXT_ETIQUETA_AUXILIAR,
                           sub.TEMPLATE_ETIQUETA_COM_COD, sub.TEXT_ETIQUETA_COM_COD, sub.REEN_AUTO_LOJA,
                           sub.LOJA_A_MOSTRAR, sub.PICKUP_EXPEDITOR, sub.PICKUP_DESTINATARIO,
                           sub.MORADA_DESTINATARIO, sub.LIMITE_PESO,
                           case when al.limit_peso > 0 then al.limit_peso else TO_NUMBER(sub.PESO) end as PESO,
                           sub.INFLIGHT_LOJAS, sub.SE_INFLIGHT_LOJAS_LIM_VOL, sub.SE_INFLIGHT_LOJAS_LIM_P,
                           sub.INFIGHT_DATA, sub.INFLIGHT_MORADA, sub.SWAP,
                           case when al.TIPO_DESTINO > 0 then al.TIPO_DESTINO else sub.TIPO_DESTINO end as TIPO_DESTINO,
                           sub.DIMENSOES, sub.MEDIDAS, sub.LOCKREPPIC, sub.RECPRTDPDC, sub.DESC_SERVICO,
                           sub.FRESH, sub.SERVICE_FRIO, sub.FLAGPORTAL,
                           NVL(AL.PRT_LABEL_DPD,'0') PRT_LABEL_DPD
                      from WSDPD.AS400_SUBCONTAS sub
                      left join WSDPD.contas_fromto f on f.srv_from = sub.service_rc
                      left join WSDPD.ACCOUNT_ALTERNATE_CONFIG AL on (al.EMENT = sub.EMENT and al.EMEND = sub.EMEND)
                     where sub.EMENT = :ement
                       and (:emend is null or sub.EMEND = :emend)
                     order by sub.EMEND";

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = new OracleCommand(sql, ligacao) { BindByName = true };
        cmd.CommandTimeout = _opcoes.TimeoutSegundos;
        cmd.Parameters.Add(":ement", eeent);
        cmd.Parameters.Add(":emend", (object?)(string.IsNullOrWhiteSpace(emend) ? null : emend.Trim()) ?? DBNull.Value);

        var lista = new List<SubContaEnvio>();
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
        {
            var item = Ler<SubContaEnvio>(leitor, ColunasSubConta);
            item.PesoNumerico = Decimal(leitor, "PESO");
            item.PrtLabelDpd = Texto(leitor, "PRT_LABEL_DPD");
            lista.Add(item);
        }

        return lista;
    }

    // ------------------------------------------------------- SQL a partir do mapa

    private static string Colunas(PropriedadeColuna[] mapa) => string.Join(", ", mapa.Select(c => c.Coluna));

    private static T Ler<T>(OracleDataReader leitor, PropriedadeColuna[] mapa) where T : new()
    {
        var destino = new T();

        foreach (var col in mapa)
        {
            var ordinal = leitor.GetOrdinal(col.Coluna);
            if (leitor.IsDBNull(ordinal)) continue;

            var valor = leitor.GetValue(ordinal);
            var tipo = Nullable.GetUnderlyingType(col.Propriedade.PropertyType) ?? col.Propriedade.PropertyType;

            // As colunas do AS400 são CHAR de tamanho fixo: sem o Trim, tudo o que sai daqui vem
            // com brancos à direita e chega assim ao ecrã e ao portal.
            object convertido = tipo == typeof(string)
                ? valor.ToString()!.TrimEnd()
                : Convert.ChangeType(valor, tipo);

            col.Propriedade.SetValue(destino, convertido);
        }

        return destino;
    }

    private async Task InserirAsync(OracleConnection ligacao, string tabela, PropriedadeColuna[] mapa,
        object origem, CancellationToken ct)
    {
        var campos = mapa.Where(c => !c.Chave).ToArray();

        try
        {
            await ExecutarInsercaoAsync(ligacao, tabela, campos, origem, null, ct);
        }
        catch (OracleException ex) when (ex.Number == 1400)
        {
            // ORA-01400: a tabela exige IDT e não tem sequência nem trigger a preenchê-lo.
            // As tabelas são alimentadas pelo AS400 e o IDT nem sempre é gerado do lado do
            // Oracle — quando não é, calculamo-lo aqui a partir do máximo existente.
            await ExecutarInsercaoAsync(ligacao, tabela, campos, origem,
                $"(SELECT NVL(MAX(IDT),0)+1 FROM {tabela})", ct);
        }
    }

    private async Task ExecutarInsercaoAsync(OracleConnection ligacao, string tabela, PropriedadeColuna[] campos,
        object origem, string? expressaoIdt, CancellationToken ct)
    {
        var colunas = campos.Select(c => c.Coluna).ToList();
        var valores = campos.Select(c => ":" + c.Coluna).ToList();

        if (expressaoIdt is not null)
        {
            colunas.Insert(0, "IDT");
            valores.Insert(0, expressaoIdt);
        }

        var sql = $"INSERT INTO {tabela} ({string.Join(", ", colunas)}) VALUES ({string.Join(", ", valores)})";

        await using var cmd = new OracleCommand(sql, ligacao) { BindByName = true, CommandTimeout = _opcoes.TimeoutSegundos };
        foreach (var c in campos)
            cmd.Parameters.Add(":" + c.Coluna, c.Propriedade.GetValue(origem) ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task AtualizarAsync(OracleConnection ligacao, string tabela, PropriedadeColuna[] mapa,
        object origem, string filtro, OracleParameter[] parametrosFiltro, CancellationToken ct)
    {
        var campos = mapa.Where(c => !c.Chave).ToArray();
        var sql = $"UPDATE {tabela} SET {string.Join(", ", campos.Select(c => $"{c.Coluna} = :{c.Coluna}"))} WHERE {filtro}";

        await using var cmd = new OracleCommand(sql, ligacao) { BindByName = true, CommandTimeout = _opcoes.TimeoutSegundos };
        foreach (var c in campos)
            cmd.Parameters.Add(":" + c.Coluna, c.Propriedade.GetValue(origem) ?? DBNull.Value);
        foreach (var p in parametrosFiltro)
            cmd.Parameters.Add(p);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static string? Texto(OracleDataReader leitor, string coluna)
    {
        var i = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(i) ? null : leitor.GetValue(i).ToString()!.TrimEnd();
    }

    private static decimal? Decimal(OracleDataReader leitor, string coluna)
    {
        var i = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(i) ? null : Convert.ToDecimal(leitor.GetValue(i));
    }
}
