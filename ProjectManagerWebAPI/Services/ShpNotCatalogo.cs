using System.Text.Json;
using ProjectManagerWebAPI.Models.ShpNot;

namespace ProjectManagerWebAPI.Services;

/// <summary>
/// O dicionário de campos do SHPNOT: para cada coluna de cada tabela, o nome que o campo tem
/// no JSON recebido e o alias com que sai na <c>GROUPSHPNOT.VW_SHPNOT_AS400</c>.
///
/// <para>Vem de <c>Data/ShpNot/shpnot-campos.json</c>, gerado a partir de três origens que não
/// se podem consultar em tempo de execução sem custo: o snapshot do EF do WebApiShpNot (que
/// tabelas e colunas existem), o contrato OpenAPI dos SHPNOTs (o nome em JSON, que <b>não</b>
/// está no código — o WebApiShpNot não usa <c>JsonPropertyName</c>, casa por comparação sem
/// maiúsculas) e o DDL da view (o alias). O ficheiro está versionado ao lado do SQL da view,
/// em <c>vw_shpnot_as400.sql</c>, para se poder refazer quando o contrato mudar.</para>
///
/// <para>Uma coluna sem entrada no dicionário continua a aparecer no ecrã — só sem nome de
/// JSON nem alias. É de propósito: um campo novo na base tem de se ver, e não desaparecer
/// por o dicionário estar desatualizado.</para>
/// </summary>
public interface IShpNotCatalogo
{
    /// <summary>Descreve uma coluna. Nunca devolve nulo: inventa a descrição mínima.</summary>
    CampoShpNot Descrever(string tabela, string coluna, string? valor, bool lista = false);

    /// <summary>As colunas conhecidas de uma tabela, pela ordem do dicionário.</summary>
    IReadOnlyList<string> ColunasDe(string tabela);
}

public sealed class ShpNotCatalogo : IShpNotCatalogo
{
    /// <summary>
    /// Colunas guardadas como texto JSON pelos conversores do EF no WebApiShpNot. Chegam à
    /// base como <c>["a","b"]</c>; o ecrã mostra-as como lista.
    /// </summary>
    private static readonly HashSet<string> Listas =
    [
        "SHPNOTIN.SPARTNERREFS", "SHPNOTIN.MPSCREFS", "DELIVERY.PODINFOS",
        "INTERNATIONAL.CPAPERS", "INTERNATIONAL.CINVOICEDATES",
        "PARCEL.SENDPARCELREFS", "PARCEL.PPARTNERREFS",
    ];

    private readonly Dictionary<string, Dictionary<string, Entrada>> _tabelas;

    private sealed record Entrada(string? Json, string? Alias);

    public ShpNotCatalogo(IWebHostEnvironment ambiente, ILogger<ShpNotCatalogo> logger)
    {
        var caminho = Path.Combine(ambiente.ContentRootPath, "Data", "ShpNot", "shpnot-campos.json");
        _tabelas = new(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(caminho))
        {
            logger.LogWarning("Dicionário de campos do ShpNot não encontrado em {Caminho}; " +
                              "o ecrã mostra as colunas sem nome de JSON nem alias.", caminho);
            return;
        }

        using var documento = JsonDocument.Parse(File.ReadAllText(caminho));
        foreach (var tabela in documento.RootElement.EnumerateObject())
        {
            var colunas = new Dictionary<string, Entrada>(StringComparer.OrdinalIgnoreCase);
            foreach (var coluna in tabela.Value.EnumerateObject())
            {
                colunas[coluna.Name] = new Entrada(
                    coluna.Value.TryGetProperty("json", out var j) ? j.GetString() : null,
                    coluna.Value.TryGetProperty("alias", out var a) ? a.GetString() : null);
            }
            _tabelas[tabela.Name] = colunas;
        }
    }

    public CampoShpNot Descrever(string tabela, string coluna, string? valor, bool lista = false)
    {
        Entrada? entrada = null;
        if (_tabelas.TryGetValue(tabela, out var colunas) && colunas.TryGetValue(coluna, out var e))
            entrada = e;

        return new CampoShpNot
        {
            Etiqueta = entrada?.Json ?? coluna,
            Json = entrada?.Json,
            Tabela = tabela.ToUpperInvariant(),
            Coluna = coluna.ToUpperInvariant(),
            Alias = entrada?.Alias,
            Valor = valor,
            Lista = lista || Listas.Contains($"{tabela}.{coluna}".ToUpperInvariant()),
        };
    }

    public IReadOnlyList<string> ColunasDe(string tabela) =>
        _tabelas.TryGetValue(tabela, out var colunas) ? [.. colunas.Keys] : [];
}
