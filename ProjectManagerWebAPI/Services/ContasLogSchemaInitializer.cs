using Oracle.ManagedDataAccess.Client;

namespace ProjectManagerWebAPI.Services;

/// <summary>
/// Cria as tabelas de registo de envios da Gestão de Dados, à imagem do
/// <see cref="OraConsoleLogSchemaInitializer"/>: um CREATE que engole o ORA-955 (já existe).
///
/// Ficam no esquema DPDIT (a ligação OraConsoleAudit), e não em WSDPD: WSDPD é do AS400 e as
/// suas tabelas não são nossas para lá acrescentar registo de aplicação.
///
/// A coluna da resposta do portal é <b>NCLOB</b> e não CLOB de propósito: o CLOB usa o charset
/// da base (não Unicode) e um acento ou um travessão de uma mensagem de erro do portal fica "¿".
/// </summary>
public static class ContasLogSchemaInitializer
{
    private const string CreateCabecalho = @"
BEGIN
  EXECUTE IMMEDIATE 'CREATE TABLE DPDIT.CONTAS_ENVIO_LOG (
    ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    UTILIZADOR VARCHAR2(256),
    AMBIENTE VARCHAR2(10) NOT NULL,
    CONTA VARCHAR2(50) NOT NULL,
    SUBCONTA VARCHAR2(50),
    SUCESSO NUMBER(1) DEFAULT 0,
    TOTAL_LINHAS NUMBER DEFAULT 0,
    FALHAS NUMBER DEFAULT 0,
    MENSAGEM VARCHAR2(1000),
    ELAPSED_MS NUMBER,
    CRIADO_EM TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
  )';
EXCEPTION
  WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
END;";

    private const string CreateLinha = @"
BEGIN
  EXECUTE IMMEDIATE 'CREATE TABLE DPDIT.CONTAS_ENVIO_LOG_LINHA (
    ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    LOG_ID NUMBER NOT NULL,
    TIPO VARCHAR2(20),
    IDENTIFICACAO VARCHAR2(100),
    METODO VARCHAR2(10),
    STATUS_CODE NUMBER,
    SUCESSO NUMBER(1) DEFAULT 0,
    RESPOSTA NCLOB,
    CRIADO_EM TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
  )';
EXCEPTION
  WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
END;";

    public static void EnsureLogTables(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OraConsoleAudit");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("⚠️ ConnectionStrings:OraConsoleAudit não configurado — tabelas de log de envios não verificadas");
            return;
        }

        using var connection = new OracleConnection(connectionString);
        connection.Open();

        foreach (var sql in new[] { CreateCabecalho, CreateLinha })
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        Console.WriteLine("✅ Tabelas DPDIT.CONTAS_ENVIO_LOG(_LINHA) verificadas/criadas");
    }
}
