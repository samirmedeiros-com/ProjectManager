using Oracle.ManagedDataAccess.Client;

namespace ProjectManagerWebAPI.Services;

public interface IOraConsoleAuditLogService
{
    Task LogLoginAsync(string username);
    Task LogQueryAsync(string username, string sqlText, bool success, string? errorMessage, int? affectedRows, long elapsedMs);
}

public class OraConsoleAuditLogService : IOraConsoleAuditLogService
{
    private readonly string? _connectionString;

    public OraConsoleAuditLogService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("OraConsoleAudit");
    }

    public Task LogLoginAsync(string username)
        => InsertLogAsync(username, "LOGIN", null, true, null, null, null);

    public Task LogQueryAsync(string username, string sqlText, bool success, string? errorMessage, int? affectedRows, long elapsedMs)
        => InsertLogAsync(username, "QUERY", sqlText, success, errorMessage, affectedRows, elapsedMs);

    private async Task InsertLogAsync(string username, string eventType, string? sqlText, bool success, string? errorMessage, int? affectedRows, long? elapsedMs)
    {
        if (string.IsNullOrWhiteSpace(_connectionString)) return;

        try
        {
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO DPDIT.ORACONSOLE_LOG
                (ORA_USERNAME, EVENT_TYPE, SQL_TEXT, SUCCESS, ERROR_MESSAGE, AFFECTED_ROWS, ELAPSED_MS)
                VALUES (:username, :eventType, :sqlText, :success, :errorMessage, :affectedRows, :elapsedMs)";
            command.Parameters.Add(new OracleParameter("username", username));
            command.Parameters.Add(new OracleParameter("eventType", eventType));
            command.Parameters.Add(new OracleParameter("sqlText", OracleDbType.Clob) { Value = (object?)sqlText ?? DBNull.Value });
            command.Parameters.Add(new OracleParameter("success", success ? 1 : 0));
            command.Parameters.Add(new OracleParameter("errorMessage", (object?)errorMessage ?? DBNull.Value));
            command.Parameters.Add(new OracleParameter("affectedRows", (object?)affectedRows ?? DBNull.Value));
            command.Parameters.Add(new OracleParameter("elapsedMs", (object?)elapsedMs ?? DBNull.Value));
            await command.ExecuteNonQueryAsync();
            connection.Commit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Falha ao gravar log OraConsole: {ex.Message}");
        }
    }
}
