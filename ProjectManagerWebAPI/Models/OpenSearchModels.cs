using System.Text.Json;

namespace ProjectManagerWebAPI.Models.OpenSearch;

/// <summary>Estado geral do cluster, usado nos cartões de métricas do portal.</summary>
public sealed record EstadoCluster(
    string Nome,
    string Versao,
    string Saude,
    int TotalIndices,
    long TotalDocumentos,
    long TamanhoBytes);

/// <summary>Um índice disponível para consulta.</summary>
public sealed record IndiceInfo(
    string Nome,
    string Saude,
    string Estado,
    long Documentos,
    long TamanhoBytes);

/// <summary>Um campo do mapping de um índice, achatado em caminho com pontos.</summary>
public sealed record CampoInfo(
    string Nome,
    string Tipo,
    bool Ordenavel,
    bool Temporal);

/// <summary>Pedido de pesquisa vindo do portal.</summary>
public sealed class PedidoPesquisa
{
    public string Indice { get; set; } = "";
    public string? Consulta { get; set; }
    public string? CampoData { get; set; }
    public DateTimeOffset? De { get; set; }
    public DateTimeOffset? Ate { get; set; }
    public string? OrdenarPor { get; set; }
    public bool OrdemDescendente { get; set; } = true;
    public int Pagina { get; set; }
    public int Tamanho { get; set; } = 25;
}

/// <summary>Um documento devolvido pela pesquisa, com os campos já achatados.</summary>
public sealed record DocumentoResultado(
    string Id,
    string Indice,
    double? Score,
    Dictionary<string, JsonElement?> Campos);

/// <summary>Resposta da pesquisa: documentos, colunas sugeridas e metadados de paginação.</summary>
public sealed record ResultadoPesquisa(
    long Total,
    bool TotalExato,
    long DuracaoMs,
    IReadOnlyList<string> Colunas,
    IReadOnlyList<DocumentoResultado> Documentos);
