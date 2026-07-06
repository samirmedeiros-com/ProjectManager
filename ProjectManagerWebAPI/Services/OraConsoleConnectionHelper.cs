namespace ProjectManagerWebAPI.Services;

public static class OraConsoleConnectionHelper
{
    public static string BuildConnectionString(IConfiguration configuration, string username, string password)
    {
        var dataSource = configuration["OraConsole:DataSource"]
            ?? throw new InvalidOperationException("OraConsole:DataSource não configurado");
        return $"Data Source={dataSource};User Id={username};Password={password};";
    }
}
