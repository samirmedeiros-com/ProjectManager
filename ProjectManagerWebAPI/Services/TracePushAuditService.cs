using Oracle.ManagedDataAccess.Client;
using ProjectManagerWebAPI.Models.TracePush;

namespace ProjectManagerWebAPI.Services;

public interface ITracePushAuditService
{
    /// <summary>Grava uma linha por push reposto na fila. Nunca lança.</summary>
    Task RegistarReenvioAsync(string utilizador, IReadOnlyCollection<PushResumo> linhas, bool sucesso, string? mensagem);

    Task<List<ReenvioLog>> ListarAsync(int limite, string? userLogin = null, string? guia = null);
}

/// <summary>
/// Registo dos reenvios, no esquema DPDIT (ligação OraConsoleAudit), ao lado do registo de
/// envios da Gestão de Dados.
///
/// Guarda-se a <b>flag anterior</b> de cada linha e não só o facto de ter sido reposta: é a
/// única coisa que se perde no UPDATE, e é justamente a que responde à pergunta que se faz
/// depois — "isto foi reenviado porque tinha dado erro, ou alguém reenviou um envio que já
/// tinha corrido bem?".
///
/// Falhar a gravar o registo nunca faz falhar o reenvio: nessa altura as linhas já estão
/// repostas na fila e dizer que a operação falhou levaria alguém a repeti-la.
/// </summary>
public class TracePushAuditService(IConfiguration configuration, ILogger<TracePushAuditService> logger)
    : ITracePushAuditService
{
    private readonly string? _connectionString = configuration.GetConnectionString("OraConsoleAudit");

    public async Task RegistarReenvioAsync(string utilizador, IReadOnlyCollection<PushResumo> linhas, bool sucesso, string? mensagem)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || linhas.Count == 0) return;

        try
        {
            await using var ligacao = new OracleConnection(_connectionString);
            await ligacao.OpenAsync();

            foreach (var l in linhas)
            {
                await using var cmd = ligacao.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = @"INSERT INTO DPDIT.TRACEPUSH_REENVIO_LOG
                    (UTILIZADOR, HHPROWID, GUIA, USERLOGIN, CONTA, FLAG_ANTERIOR, SUCESSO, MENSAGEM)
                    VALUES (:utilizador, :hhprowid, :guia, :userlogin, :conta, :flag, :sucesso, :mensagem)";

                cmd.Parameters.Add(new OracleParameter("utilizador", (object?)Cortar(utilizador, 256) ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("hhprowid", l.HhpRowId));
                cmd.Parameters.Add(new OracleParameter("guia", (object?)l.Guia ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("userlogin", (object?)l.UserLogin ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("conta", (object?)l.Conta ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("flag", (object?)l.Flag ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("sucesso", sucesso ? 1 : 0));
                cmd.Parameters.Add(new OracleParameter("mensagem", (object?)Cortar(mensagem, 1000) ?? DBNull.Value));

                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Não foi possível registar o reenvio de {Total} linhas", linhas.Count);
        }
    }

    public async Task<List<ReenvioLog>> ListarAsync(int limite, string? userLogin = null, string? guia = null)
    {
        if (string.IsNullOrWhiteSpace(_connectionString)) return [];

        limite = limite is < 1 or > 500 ? 100 : limite;

        var condicoes = new List<string>();
        if (!string.IsNullOrWhiteSpace(userLogin)) condicoes.Add("USERLOGIN = :userlogin");
        if (!string.IsNullOrWhiteSpace(guia)) condicoes.Add("GUIA = :guia");
        var where = condicoes.Count > 0 ? "WHERE " + string.Join(" AND ", condicoes) : "";

        await using var ligacao = new OracleConnection(_connectionString);
        await ligacao.OpenAsync();

        await using var cmd = ligacao.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
SELECT ID, UTILIZADOR, HHPROWID, GUIA, USERLOGIN, CONTA, FLAG_ANTERIOR, SUCESSO, MENSAGEM, CRIADO_EM
FROM DPDIT.TRACEPUSH_REENVIO_LOG
{where}
ORDER BY ID DESC
FETCH FIRST :limite ROWS ONLY";

        if (!string.IsNullOrWhiteSpace(userLogin)) cmd.Parameters.Add(new OracleParameter("userlogin", userLogin.Trim()));
        if (!string.IsNullOrWhiteSpace(guia)) cmd.Parameters.Add(new OracleParameter("guia", guia.Trim()));
        cmd.Parameters.Add(new OracleParameter("limite", limite));

        var linhas = new List<ReenvioLog>();
        await using var leitor = await cmd.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
        {
            linhas.Add(new ReenvioLog
            {
                Id = Convert.ToInt64(leitor.GetValue(0)),
                Utilizador = Texto(leitor, 1),
                HhpRowId = Texto(leitor, 2) ?? "",
                Guia = Texto(leitor, 3),
                UserLogin = Texto(leitor, 4),
                Conta = Texto(leitor, 5),
                FlagAnterior = Texto(leitor, 6),
                Sucesso = !leitor.IsDBNull(7) && Convert.ToInt32(leitor.GetValue(7)) == 1,
                Mensagem = Texto(leitor, 8),
                CriadoEm = leitor.GetDateTime(9),
            });
        }
        return linhas;
    }

    private static string? Texto(OracleDataReader leitor, int i) => leitor.IsDBNull(i) ? null : leitor.GetString(i).Trim();

    private static string? Cortar(string? texto, int max) =>
        string.IsNullOrEmpty(texto) ? texto : texto.Length <= max ? texto : texto[..max];
}
