namespace ProjectManagerWebAPI.Services;

/// <summary>
/// Endpoints e credenciais do portal de clientes (business.dpd.pt), por ambiente.
/// Vem do serviço CONTASPORTAL do appsettings.secrets.json da aplicação de linha
/// de comandos: os endereços e o fluxo de autenticação são os mesmos, muda só quem
/// dispara o envio — aqui é uma pessoa a carregar num botão, lá é um ciclo infinito.
/// </summary>
public sealed class ContasOptions
{
    public const string Seccao = "ContasPortal";

    /// <summary>Ligação Oracle ao esquema WSDPD (tabelas AS400_CONTAS/AS400_SUBCONTAS).</summary>
    public string ConnectionStringNome { get; set; } = "Wsdpd";

    public int TimeoutSegundos { get; set; } = 60;

    /// <summary>Tecto de linhas devolvidas por uma pesquisa de contas.</summary>
    public int MaxLinhas { get; set; } = 200;

    public ContasAmbiente Prd { get; set; } = new();

    public ContasAmbiente Qua { get; set; } = new();

    public ContasAmbiente? Obter(string ambiente) => ambiente?.ToUpperInvariant() switch
    {
        "PRD" => Prd,
        "QUA" => Qua,
        _ => null
    };
}

public sealed class ContasAmbiente
{
    /// <summary>Endpoint OAuth. O token pedido a PRD não serve em QUA e vice-versa.</summary>
    public string AuthEndpoint { get; set; } = "";

    public string ClientId { get; set; } = "";

    public string ClientSecret { get; set; } = "";

    public string GrantType { get; set; } = "password";

    public string Utilizador { get; set; } = "";

    public string Password { get; set; } = "";

    public string ContasEndpoint { get; set; } = "";

    public string SubContasEndpoint { get; set; } = "";
}
