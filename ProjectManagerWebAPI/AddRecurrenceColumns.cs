using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;

class AddRecurrenceColumnsProgram
{
    static async Task Main(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseOracle("Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=10.2.3.30)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=CRHPOTS01.databasesubnet.vcndpd.oraclevcn.com)));User Id=samir;Password=dpd#2026;");

        using (var context = new ApplicationDbContext(optionsBuilder.Options))
        {
            try
            {
                Console.WriteLine("✅ Conectando ao Oracle...");
                await context.Database.OpenConnectionAsync();
                Console.WriteLine("✅ Conectado ao Oracle!");

                var columns = new[]
                {
                    ("ISRECURRENCEPARENT", "ALTER TABLE EVENTS ADD ISRECURRENCEPARENT NUMBER(1) DEFAULT 0"),
                    ("PARENTEVENTID", "ALTER TABLE EVENTS ADD PARENTEVENTID NUMBER(10)"),
                    ("RECURRENCEDAYSOFWEEK", "ALTER TABLE EVENTS ADD RECURRENCEDAYSOFWEEK VARCHAR2(100)"),
                    ("RECURRENCEENDCOUNT", "ALTER TABLE EVENTS ADD RECURRENCEENDCOUNT NUMBER(10)"),
                    ("RECURRENCEENDDATE", "ALTER TABLE EVENTS ADD RECURRENCEENDDATE TIMESTAMP(7)"),
                    ("RECURRENCETYPE", "ALTER TABLE EVENTS ADD RECURRENCETYPE VARCHAR2(50)")
                };

                foreach (var (name, sql) in columns)
                {
                    try
                    {
                        await context.Database.ExecuteSqlRawAsync(sql);
                        Console.WriteLine($"✓ {name} criado");
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("1430") || ex.Message.Contains("already exists"))
                        {
                            Console.WriteLine($"! {name}: coluna já existe");
                        }
                        else
                        {
                            Console.WriteLine($"✗ {name}: {ex.Message}");
                        }
                    }
                }

                await context.Database.ExecuteSqlRawAsync("COMMIT");
                Console.WriteLine("\n✅ Todas as colunas de recorrência foram processadas!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro: {ex.Message}");
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }
}
