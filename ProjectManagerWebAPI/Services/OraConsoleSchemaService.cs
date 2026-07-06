using Oracle.ManagedDataAccess.Client;
using ProjectManagerWebAPI.DTOs;

namespace ProjectManagerWebAPI.Services;

public interface IOraConsoleSchemaService
{
    Task<List<OraConsoleSchemaDto>> GetSchemasAsync(string sessionId);
    Task<List<OraConsoleTableDto>> GetTablesAsync(string sessionId, string owner);
    Task<List<OraConsoleColumnDto>> GetColumnsAsync(string sessionId, string owner, string tableName);
}

public class OraConsoleSchemaService : IOraConsoleSchemaService
{
    private readonly IConfiguration _configuration;
    private readonly IOraConsoleSessionStore _sessionStore;

    public OraConsoleSchemaService(IConfiguration configuration, IOraConsoleSessionStore sessionStore)
    {
        _configuration = configuration;
        _sessionStore = sessionStore;
    }

    public async Task<List<OraConsoleSchemaDto>> GetSchemasAsync(string sessionId)
    {
        await using var connection = await OpenConnectionAsync(sessionId);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT OWNER FROM ALL_TABLES ORDER BY OWNER";

        var result = new List<OraConsoleSchemaDto>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OraConsoleSchemaDto { Owner = reader.GetString(0) });
        return result;
    }

    public async Task<List<OraConsoleTableDto>> GetTablesAsync(string sessionId, string owner)
    {
        await using var connection = await OpenConnectionAsync(sessionId);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TABLE_NAME FROM ALL_TABLES WHERE OWNER = :owner ORDER BY TABLE_NAME";
        command.Parameters.Add(new OracleParameter("owner", owner));

        var result = new List<OraConsoleTableDto>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OraConsoleTableDto { TableName = reader.GetString(0) });
        return result;
    }

    public async Task<List<OraConsoleColumnDto>> GetColumnsAsync(string sessionId, string owner, string tableName)
    {
        await using var connection = await OpenConnectionAsync(sessionId);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT COLUMN_NAME, DATA_TYPE, DATA_LENGTH, NULLABLE, DATA_DEFAULT, COLUMN_ID
                                 FROM ALL_TAB_COLUMNS
                                 WHERE OWNER = :owner AND TABLE_NAME = :tableName
                                 ORDER BY COLUMN_ID";
        command.Parameters.Add(new OracleParameter("owner", owner));
        command.Parameters.Add(new OracleParameter("tableName", tableName));

        var result = new List<OraConsoleColumnDto>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new OraConsoleColumnDto
            {
                ColumnName = reader.GetString(0),
                DataType = reader.GetString(1),
                DataLength = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Nullable = reader.IsDBNull(3) ? "Y" : reader.GetString(3),
                DataDefault = reader.IsDBNull(4) ? null : reader.GetString(4).Trim(),
                ColumnId = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5))
            });
        }
        return result;
    }

    private async Task<OracleConnection> OpenConnectionAsync(string sessionId)
    {
        if (!_sessionStore.TryGetCredentials(sessionId, out var credentials))
            throw new OraConsoleException("Sessão Oracle expirada. Por favor autentique-se novamente.");

        var connectionString = OraConsoleConnectionHelper.BuildConnectionString(_configuration, credentials.Username, credentials.Password);
        var connection = new OracleConnection(connectionString);
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch (OracleException ex)
        {
            await connection.DisposeAsync();
            throw new OraConsoleException($"Erro Oracle ({ex.Number}): {ex.Message}");
        }
    }
}
