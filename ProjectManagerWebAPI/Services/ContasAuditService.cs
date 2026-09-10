using Oracle.ManagedDataAccess.Client;
using ProjectManagerWebAPI.Models.Contas;

namespace ProjectManagerWebAPI.Services;

public interface IContasAuditService
{
    /// <summary>Grava a operação de envio e as respostas de cada conta/subconta. Nunca lança.</summary>
    Task RegistarEnvioAsync(string utilizador, PedidoEnvio pedido, ResultadoEnvio resultado, long elapsedMs);

    Task<List<EnvioLog>> ListarAsync(int limite, string? conta = null);
    Task<List<EnvioLogLinha>> LinhasAsync(long logId);
}

/// <summary>
/// Registo persistente dos envios ao portal, no esquema DPDIT (ligação OraConsoleAudit).
///
/// Um cabeçalho por operação (quem, ambiente, conta, resultado) e uma linha por resposta de
/// conta ou subconta — a mesma informação que aparece no ecrã depois do envio, mas guardada,
/// para se poder responder mais tarde "o que é que o portal disse quando isto foi enviado".
///
/// Uma falha a gravar o log nunca pode fazer falhar o envio: o envio já aconteceu do lado do
/// portal quando isto corre, e perder o registo é mau, mas fingir que o envio falhou é pior.
/// </summary>
public class ContasAuditService(IConfiguration configuration, ILogger<ContasAuditService> logger)
    : IContasAuditService
{
    private readonly string? _connectionString = configuration.GetConnectionString("OraConsoleAudit");

    public async Task RegistarEnvioAsync(string utilizador, PedidoEnvio pedido, ResultadoEnvio resultado, long elapsedMs)
    {
        if (string.IsNullOrWhiteSpace(_connectionString)) return;

        try
        {
            await using var ligacao = new OracleConnection(_connectionString);
            await ligacao.OpenAsync();

            long logId;
            await using (var cabecalho = ligacao.CreateCommand())
            {
                cabecalho.BindByName = true;
                cabecalho.CommandText = @"INSERT INTO DPDIT.CONTAS_ENVIO_LOG
                    (UTILIZADOR, AMBIENTE, CONTA, SUBCONTA, SUCESSO, TOTAL_LINHAS, FALHAS, MENSAGEM, ELAPSED_MS)
                    VALUES (:utilizador, :ambiente, :conta, :subconta, :sucesso, :total, :falhas, :mensagem, :elapsed)
                    RETURNING ID INTO :id";

                cabecalho.Parameters.Add(new OracleParameter("utilizador", (object?)utilizador ?? DBNull.Value));
                cabecalho.Parameters.Add(new OracleParameter("ambiente", resultado.Ambiente));
                cabecalho.Parameters.Add(new OracleParameter("conta", pedido.Conta));
                cabecalho.Parameters.Add(new OracleParameter("subconta",
                    (object?)(string.IsNullOrWhiteSpace(pedido.SubConta) ? null : pedido.SubConta) ?? DBNull.Value));
                cabecalho.Parameters.Add(new OracleParameter("sucesso", resultado.Sucesso ? 1 : 0));
                cabecalho.Parameters.Add(new OracleParameter("total", resultado.Linhas.Count));
                cabecalho.Parameters.Add(new OracleParameter("falhas", resultado.Linhas.Count(l => !l.Sucesso)));
                cabecalho.Parameters.Add(new OracleParameter("mensagem", (object?)Cortar(resultado.Mensagem, 1000) ?? DBNull.Value));
                cabecalho.Parameters.Add(new OracleParameter("elapsed", elapsedMs));

                var idParam = new OracleParameter("id", OracleDbType.Int64) { Direction = System.Data.ParameterDirection.Output };
                cabecalho.Parameters.Add(idParam);

                await cabecalho.ExecuteNonQueryAsync();
                logId = ((Oracle.ManagedDataAccess.Types.OracleDecimal)idParam.Value).ToInt64();
            }

            foreach (var l in resultado.Linhas)
            {
                await using var linha = ligacao.CreateCommand();
                linha.BindByName = true;
                linha.CommandText = @"INSERT INTO DPDIT.CONTAS_ENVIO_LOG_LINHA
                    (LOG_ID, TIPO, IDENTIFICACAO, METODO, STATUS_CODE, SUCESSO, RESPOSTA)
                    VALUES (:logId, :tipo, :ident, :metodo, :status, :sucesso, :resposta)";

                linha.Parameters.Add(new OracleParameter("logId", logId));
                linha.Parameters.Add(new OracleParameter("tipo", l.Tipo));
                linha.Parameters.Add(new OracleParameter("ident", l.Identificacao));
                linha.Parameters.Add(new OracleParameter("metodo", l.Metodo));
                linha.Parameters.Add(new OracleParameter("status", l.StatusCode));
                linha.Parameters.Add(new OracleParameter("sucesso", l.Sucesso ? 1 : 0));
                // NClob: as respostas de erro do portal trazem acentos e travessões.
                linha.Parameters.Add(new OracleParameter("resposta", OracleDbType.NClob)
                {
                    Value = (object?)l.Resposta ?? DBNull.Value
                });

                await linha.ExecuteNonQueryAsync();
            }

            ligacao.Commit();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao gravar o registo do envio da conta {Conta} ({Ambiente})",
                pedido.Conta, resultado.Ambiente);
        }
    }

    public async Task<List<EnvioLog>> ListarAsync(int limite, string? conta = null)
    {
        var lista = new List<EnvioLog>();
        if (string.IsNullOrWhiteSpace(_connectionString)) return lista;

        await using var ligacao = new OracleConnection(_connectionString);
        await ligacao.OpenAsync();
        await using var cmd = ligacao.CreateCommand();
        cmd.BindByName = true;
        // O filtro por conta serve o registo mostrado dentro do detalhe de uma conta.
        cmd.CommandText = @"SELECT * FROM (
                              SELECT ID, UTILIZADOR, AMBIENTE, CONTA, SUBCONTA, SUCESSO,
                                     TOTAL_LINHAS, FALHAS, MENSAGEM, ELAPSED_MS, CRIADO_EM
                                FROM DPDIT.CONTAS_ENVIO_LOG
                               WHERE (:conta IS NULL OR CONTA = :conta)
                               ORDER BY ID DESC)
                            WHERE ROWNUM <= :limite";
        cmd.Parameters.Add(new OracleParameter("conta",
            (object?)(string.IsNullOrWhiteSpace(conta) ? null : conta.Trim()) ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("limite", limite <= 0 ? 100 : limite));

        await using var leitor = await cmd.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
        {
            lista.Add(new EnvioLog
            {
                Id = Convert.ToInt64(leitor["ID"]),
                Utilizador = leitor["UTILIZADOR"] as string,
                Ambiente = leitor["AMBIENTE"] as string ?? "",
                Conta = leitor["CONTA"] as string ?? "",
                SubConta = leitor["SUBCONTA"] as string,
                Sucesso = Convert.ToInt32(leitor["SUCESSO"]) == 1,
                TotalLinhas = Convert.ToInt32(leitor["TOTAL_LINHAS"]),
                Falhas = Convert.ToInt32(leitor["FALHAS"]),
                Mensagem = leitor["MENSAGEM"] as string,
                ElapsedMs = leitor["ELAPSED_MS"] is DBNull ? null : Convert.ToInt64(leitor["ELAPSED_MS"]),
                CriadoEm = Convert.ToDateTime(leitor["CRIADO_EM"])
            });
        }

        return lista;
    }

    public async Task<List<EnvioLogLinha>> LinhasAsync(long logId)
    {
        var lista = new List<EnvioLogLinha>();
        if (string.IsNullOrWhiteSpace(_connectionString)) return lista;

        await using var ligacao = new OracleConnection(_connectionString);
        await ligacao.OpenAsync();
        await using var cmd = ligacao.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = @"SELECT TIPO, IDENTIFICACAO, METODO, STATUS_CODE, SUCESSO, RESPOSTA, CRIADO_EM
                              FROM DPDIT.CONTAS_ENVIO_LOG_LINHA
                             WHERE LOG_ID = :logId
                             ORDER BY ID";
        cmd.Parameters.Add(new OracleParameter("logId", logId));

        await using var leitor = await cmd.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
        {
            lista.Add(new EnvioLogLinha
            {
                Tipo = leitor["TIPO"] as string ?? "",
                Identificacao = leitor["IDENTIFICACAO"] as string ?? "",
                Metodo = leitor["METODO"] as string ?? "",
                StatusCode = leitor["STATUS_CODE"] is DBNull ? 0 : Convert.ToInt32(leitor["STATUS_CODE"]),
                Sucesso = Convert.ToInt32(leitor["SUCESSO"]) == 1,
                Resposta = leitor["RESPOSTA"] as string,
                CriadoEm = Convert.ToDateTime(leitor["CRIADO_EM"])
            });
        }

        return lista;
    }

    private static string? Cortar(string? texto, int max)
        => texto is null || texto.Length <= max ? texto : texto[..max];
}
