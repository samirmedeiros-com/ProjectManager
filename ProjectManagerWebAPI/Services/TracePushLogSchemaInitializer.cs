using Oracle.ManagedDataAccess.Client;

namespace ProjectManagerWebAPI.Services;

/// <summary>
/// Cria a tabela de registo de reenvios de trace push, no mesmo molde do
/// <see cref="ContasLogSchemaInitializer"/>: um CREATE que engole o ORA-955 (já existe).
///
/// Fica em DPDIT e não em CHRONO_WEB: CW_TRACEPUSH é da aplicação de push e as suas tabelas
/// não são nossas para lá acrescentar registo de portal.
///
/// MENSAGEM é NVARCHAR2 e não VARCHAR2 pela mesma razão que o registo de envios usa NCLOB —
/// o charset da base não é Unicode e um acento numa mensagem de erro sai como "¿".
/// </summary>
public static class TracePushLogSchemaInitializer
{
    private const string CreateTabela = @"
BEGIN
  EXECUTE IMMEDIATE 'CREATE TABLE DPDIT.TRACEPUSH_REENVIO_LOG (
    ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    UTILIZADOR VARCHAR2(256),
    HHPROWID VARCHAR2(50) NOT NULL,
    GUIA VARCHAR2(50),
    USERLOGIN VARCHAR2(100),
    CONTA VARCHAR2(30),
    FLAG_ANTERIOR VARCHAR2(1),
    SUCESSO NUMBER(1) DEFAULT 0,
    MENSAGEM NVARCHAR2(1000),
    CRIADO_EM TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
  )';
EXCEPTION
  WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
END;";

    /// <summary>Sem índice a lista fica lenta assim que o registo crescer — ordena-se sempre por ID DESC.</summary>
    private const string CreateIndice = @"
BEGIN
  EXECUTE IMMEDIATE 'CREATE INDEX DPDIT.IDX_TRACEPUSH_REENVIO_GUIA ON DPDIT.TRACEPUSH_REENVIO_LOG (GUIA)';
EXCEPTION
  WHEN OTHERS THEN IF SQLCODE != -955 AND SQLCODE != -1408 THEN RAISE; END IF;
END;";

    public static void EnsureLogTables(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OraConsoleAudit");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("⚠️ ConnectionStrings:OraConsoleAudit não configurado — tabela de reenvios de trace não verificada");
            return;
        }

        using var connection = new OracleConnection(connectionString);
        connection.Open();

        foreach (var sql in new[] { CreateTabela, CreateIndice })
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        Console.WriteLine("✅ Tabela DPDIT.TRACEPUSH_REENVIO_LOG verificada/criada");
    }
}
