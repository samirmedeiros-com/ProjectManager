namespace ProjectManagerWebAPI.Services;

public sealed class OpenSearchOptions
{
    public const string Seccao = "OpenSearch";

    /// <summary>URL base da API do OpenSearch (a porta 9200, não a do Dashboards).</summary>
    public string BaseUrl { get; set; } = "";

    public string Utilizador { get; set; } = "";

    public string Password { get; set; } = "";

    /// <summary>
    /// Clusters geridos costumam usar certificado auto-assinado; ligar isto ignora a validação.
    /// Só para redes internas de confiança.
    /// </summary>
    public bool IgnorarCertificado { get; set; }

    public int TimeoutSegundos { get; set; } = 30;

    /// <summary>Se falso, índices de sistema (começados por ponto) não aparecem no seletor.</summary>
    public bool IncluirIndicesSistema { get; set; }

    /// <summary>Tecto de segurança para o tamanho de página pedido pelo portal.</summary>
    public int TamanhoPaginaMaximo { get; set; } = 200;
}
