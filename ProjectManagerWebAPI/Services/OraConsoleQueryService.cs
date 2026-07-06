using System.Diagnostics;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using ProjectManagerWebAPI.DTOs;

namespace ProjectManagerWebAPI.Services;

public class OraConsoleException : Exception
{
    public OraConsoleException(string message) : base(message) { }
}

public interface IOraConsoleQueryService
{
    Task<OraConsoleQueryResult> ExecuteAsync(string sessionId, OraConsoleQueryRequest request);
    Task<OraConsoleQueryResult> UpdateCellAsync(string sessionId, OraConsoleCellUpdateRequest request);
}

public partial class OraConsoleQueryService : IOraConsoleQueryService
{
    private readonly IConfiguration _configuration;
    private readonly IOraConsoleSessionStore _sessionStore;
    private readonly IOraConsoleAuditLogService _auditLogService;
    private readonly int _maxRows;

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_$#]*$")]
    private static partial Regex IdentifierRegex();

    public OraConsoleQueryService(IConfiguration configuration, IOraConsoleSessionStore sessionStore, IOraConsoleAuditLogService auditLogService)
    {
        _configuration = configuration;
        _sessionStore = sessionStore;
        _auditLogService = auditLogService;
        _maxRows = configuration.GetValue("OraConsole:MaxRows", 500);
    }

    public async Task<OraConsoleQueryResult> ExecuteAsync(string sessionId, OraConsoleQueryRequest request)
    {
        if (!_sessionStore.TryGetCredentials(sessionId, out var credentials))
            throw new OraConsoleException("Sessão Oracle expirada. Por favor autentique-se novamente.");

        var sql = (request.Sql ?? string.Empty).Trim();
        if (sql.EndsWith(';'))
            sql = sql[..^1].TrimEnd();

        if (string.IsNullOrWhiteSpace(sql))
            throw new OraConsoleException("Instrução SQL vazia");

        var isSelect = sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || sql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase);

        var connectionString = OraConsoleConnectionHelper.BuildConnectionString(_configuration, credentials.Username, credentials.Password);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync();

            var result = isSelect
                ? await ExecuteSelectAsync(connection, sql, request, stopwatch)
                : await ExecuteNonQueryAsync(connection, sql, stopwatch);

            await _auditLogService.LogQueryAsync(credentials.Username, sql, true, null, result.AffectedRows, result.ElapsedMs);
            return result;
        }
        catch (OracleException ex)
        {
            stopwatch.Stop();
            await _auditLogService.LogQueryAsync(credentials.Username, sql, false, ex.Message, null, stopwatch.ElapsedMilliseconds);
            throw new OraConsoleException($"Erro Oracle ({ex.Number}): {ex.Message}");
        }
    }

    public async Task<OraConsoleQueryResult> UpdateCellAsync(string sessionId, OraConsoleCellUpdateRequest request)
    {
        if (!_sessionStore.TryGetCredentials(sessionId, out var credentials))
            throw new OraConsoleException("Sessão Oracle expirada. Por favor autentique-se novamente.");

        foreach (var identifier in new[] { request.Owner, request.TableName, request.ColumnName })
        {
            if (string.IsNullOrWhiteSpace(identifier) || !IdentifierRegex().IsMatch(identifier))
                throw new OraConsoleException($"Identificador inválido: '{identifier}'");
        }

        if (string.IsNullOrWhiteSpace(request.RowId))
            throw new OraConsoleException("RowId em falta");

        var sql = $"UPDATE \"{request.Owner}\".\"{request.TableName}\" SET \"{request.ColumnName}\" = :val WHERE ROWID = :rid";
        var loggedSql = $"UPDATE \"{request.Owner}\".\"{request.TableName}\" SET \"{request.ColumnName}\" = {FormatValueForLog(request.Value)} WHERE ROWID = '{request.RowId}'";

        var connectionString = OraConsoleConnectionHelper.BuildConnectionString(_configuration, credentials.Username, credentials.Password);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(new OracleParameter("val", (object?)request.Value ?? DBNull.Value));
            command.Parameters.Add(new OracleParameter("rid", request.RowId));
            var affected = await command.ExecuteNonQueryAsync();
            connection.Commit();
            stopwatch.Stop();

            await _auditLogService.LogQueryAsync(credentials.Username, loggedSql, true, null, affected, stopwatch.ElapsedMilliseconds);

            return new OraConsoleQueryResult
            {
                IsResultSet = false,
                AffectedRows = affected,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (OracleException ex)
        {
            stopwatch.Stop();
            await _auditLogService.LogQueryAsync(credentials.Username, loggedSql, false, ex.Message, null, stopwatch.ElapsedMilliseconds);
            throw new OraConsoleException($"Erro Oracle ({ex.Number}): {ex.Message}");
        }
    }

    private static string FormatValueForLog(string? value)
    {
        if (value == null) return "NULL";
        return "'" + value.Replace("'", "''") + "'";
    }

    private async Task<OraConsoleQueryResult> ExecuteSelectAsync(OracleConnection connection, string sql, OraConsoleQueryRequest request, Stopwatch stopwatch)
    {
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 100 : request.PageSize, 1, _maxRows);
        var page = Math.Max(request.Page, 1);
        var offset = (page - 1) * pageSize;

        var pagedSql = $"SELECT * FROM ({sql}) OFFSET {offset} ROWS FETCH NEXT {pageSize + 1} ROWS ONLY";

        await using var command = connection.CreateCommand();
        command.CommandText = pagedSql;
        await using var reader = await command.ExecuteReaderAsync();

        var columns = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));

        var rows = new List<Dictionary<string, object?>>();
        while (rows.Count < pageSize && await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
                row[columns[i]] = ConvertValue(reader.GetValue(i));
            rows.Add(row);
        }

        var hasMore = rows.Count == pageSize && await reader.ReadAsync();

        stopwatch.Stop();
        return new OraConsoleQueryResult
        {
            IsResultSet = true,
            Columns = columns,
            Rows = rows,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            Page = page,
            PageSize = pageSize,
            HasMore = hasMore
        };
    }

    private static async Task<OraConsoleQueryResult> ExecuteNonQueryAsync(OracleConnection connection, string sql, Stopwatch stopwatch)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var affected = await command.ExecuteNonQueryAsync();
        connection.Commit();

        stopwatch.Stop();
        return new OraConsoleQueryResult
        {
            IsResultSet = false,
            AffectedRows = affected,
            ElapsedMs = stopwatch.ElapsedMilliseconds
        };
    }

    private static object? ConvertValue(object value)
    {
        if (value is DBNull) return null;
        return value switch
        {
            OracleDecimal d => d.IsNull ? null : (object)d.Value,
            byte[] b => Convert.ToBase64String(b),
            _ => value
        };
    }
}
