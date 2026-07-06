using Oracle.ManagedDataAccess.Client;

namespace ProjectManagerWebAPI.Services;

public static class OraConsoleLogSchemaInitializer
{
    private const string CreateTableSql = @"
BEGIN
  EXECUTE IMMEDIATE 'CREATE TABLE DPDIT.ORACONSOLE_LOG (
    ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ORA_USERNAME VARCHAR2(128) NOT NULL,
    EVENT_TYPE VARCHAR2(20) NOT NULL,
    SQL_TEXT CLOB,
    SUCCESS NUMBER(1) DEFAULT 1,
    ERROR_MESSAGE VARCHAR2(4000),
    AFFECTED_ROWS NUMBER,
    ELAPSED_MS NUMBER,
    EXECUTED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
  )';
EXCEPTION
  WHEN OTHERS THEN
    IF SQLCODE != -955 THEN
      RAISE;
    END IF;
END;";

    public static void EnsureLogTable(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OraConsoleAudit");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("⚠️ ConnectionStrings:OraConsoleAudit não configurado — tabela de log OraConsole não verificada");
            return;
        }

        using var connection = new OracleConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = CreateTableSql;
        command.ExecuteNonQuery();
        Console.WriteLine("✅ Tabela DPDIT.ORACONSOLE_LOG verificada/criada");
    }
}
