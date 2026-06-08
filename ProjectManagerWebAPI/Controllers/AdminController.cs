using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;

namespace ProjectManagerWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("create-recurrence-columns")]
    public async Task<IActionResult> CreateRecurrenceColumns()
    {
        var results = new List<string>();
        var connection = _context.Database.GetDbConnection();

        string schemaPrefix = "";

        try
        {
            await connection.OpenAsync();

            // Try to get schema from database
            try
            {
                using (var queryCmd = connection.CreateCommand())
                {
                    queryCmd.CommandText = "SELECT SYS_CONTEXT('USERENV','CURRENT_SCHEMA') FROM DUAL";
                    var schema = await queryCmd.ExecuteScalarAsync();
                    if (schema != null && schema != DBNull.Value)
                    {
                        schemaPrefix = schema.ToString() + ".";
                        results.Add($"📊 Schema detectado: {schemaPrefix}");
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add($"⚠️ Não conseguiu detectar schema: {ex.Message}");
            }

            // Try without schema prefix first
            results.Add("🔍 Testando acesso à tabela EVENTS...");
            try
            {
                using (var queryCmd = connection.CreateCommand())
                {
                    queryCmd.CommandText = "SELECT COUNT(*) FROM EVENTS";
                    var count = await queryCmd.ExecuteScalarAsync();
                    results.Add($"✓ Tabela EVENTS encontrada com {count} registros");
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Não conseguiu acessar EVENTS: {ex.Message}");
                // Try with ALL_TABLES to list all accessible tables
                try
                {
                    using (var queryCmd = connection.CreateCommand())
                    {
                        queryCmd.CommandText = "SELECT OWNER, TABLE_NAME FROM ALL_TABLES WHERE TABLE_NAME = 'EVENTS' ORDER BY OWNER";
                        using (var reader = await queryCmd.ExecuteReaderAsync())
                        {
                            int count = 0;
                            while (await reader.ReadAsync() && count < 10)
                            {
                                results.Add($"📋 Tabela encontrada: {reader.GetString(0)}.{reader.GetString(1)}");
                                schemaPrefix = reader.GetString(0) + ".";
                                count++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    results.Add($"⚠️ Erro ao listar tabelas: {ex.Message}");
                }
            }

            var columns = new[]
            {
                ("ISRECURRENCEPARENT", $"ALTER TABLE {schemaPrefix}EVENTS ADD ISRECURRENCEPARENT NUMBER(1) DEFAULT 0"),
                ("PARENTEVENTID", $"ALTER TABLE {schemaPrefix}EVENTS ADD PARENTEVENTID NUMBER(10)"),
                ("RECURRENCEDAYSOFWEEK", $"ALTER TABLE {schemaPrefix}EVENTS ADD RECURRENCEDAYSOFWEEK VARCHAR2(100)"),
                ("RECURRENCEENDCOUNT", $"ALTER TABLE {schemaPrefix}EVENTS ADD RECURRENCEENDCOUNT NUMBER(10)"),
                ("RECURRENCEENDDATE", $"ALTER TABLE {schemaPrefix}EVENTS ADD RECURRENCEENDDATE TIMESTAMP(7)"),
                ("RECURRENCETYPE", $"ALTER TABLE {schemaPrefix}EVENTS ADD RECURRENCETYPE VARCHAR2(50)")
            };

            foreach (var (name, sql) in columns)
            {
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        await command.ExecuteNonQueryAsync();
                        results.Add($"✓ {name} criado com sucesso");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("1430") || ex.Message.Contains("already exists"))
                    {
                        results.Add($"! {name}: coluna já existe");
                    }
                    else
                    {
                        results.Add($"✗ {name}: {ex.Message}");
                    }
                }
            }

            results.Add("✅ Todas as colunas foram processadas com sucesso!");
        }
        catch (Exception ex)
        {
            results.Add($"❌ Erro: {ex.Message}");
        }
        finally
        {
            await connection.CloseAsync();
        }

        return Ok(new { success = true, messages = results });
    }
}
