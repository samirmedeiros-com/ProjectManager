namespace ProjectManagerWebAPI.DTOs;

public class OraConsoleLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class OraConsoleLoginResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public string? Username { get; set; }
}

public class OraConsoleQueryRequest
{
    public string Sql { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

public class OraConsoleCellUpdateRequest
{
    public string Owner { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string RowId { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class OraConsoleQueryResult
{
    public bool IsResultSet { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int? AffectedRows { get; set; }
    public long ElapsedMs { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}

public class OraConsoleSchemaDto
{
    public string Owner { get; set; } = string.Empty;
}

public class OraConsoleTableDto
{
    public string TableName { get; set; } = string.Empty;
}

public class OraConsoleColumnDto
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? DataLength { get; set; }
    public string Nullable { get; set; } = string.Empty;
    public string? DataDefault { get; set; }
    public int ColumnId { get; set; }
}
