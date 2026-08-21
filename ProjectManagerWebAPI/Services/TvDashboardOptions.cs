namespace ProjectManagerWebAPI.Services;

/// <summary>
/// Configuração do mural de TV. A secção correspondente vive no appsettings.
/// O mural não tem login: o controlo de acesso é a chave, por isso ela tem de
/// ser longa e tratada como segredo (ver <see cref="Filters.RequerChaveTvAttribute"/>).
/// </summary>
public class TvDashboardOptions
{
    public const string Seccao = "TvDashboard";

    /// <summary>
    /// Valor de origem da chave, usado quando o <c>appsettings.json</c> da máquina
    /// não traz a secção <c>TvDashboard</c>.
    ///
    /// O deploy é a cópia da publicação para o servidor, e o ficheiro de
    /// configuração que lá está é o anterior ao mural — sem este valor no código
    /// o mural chegaria a produção sem chave e responderia 401 a tudo. Com ele,
    /// o mural funciona à chegada e o <c>appsettings.json</c> continua a poder
    /// sobrepor-se, quando existir a secção.
    /// </summary>
    public const string ChavePorOmissao = "cvlFHzClh2B8Ff3Cm4Bzdd5fme6fGxXByhhoIcw2tXc";

    /// <summary>Permite desligar o mural sem remover a rota.</summary>
    public bool Ativo { get; set; } = true;

    /// <summary>Chave partilhada exigida no parâmetro <c>k</c> do URL.</summary>
    public string Chave { get; set; } = ChavePorOmissao;

    /// <summary>Intervalo de atualização sugerido ao ecrã, em segundos.</summary>
    public int RefreshSegundos { get; set; } = 60;

    /// <summary>Número máximo de linhas devolvidas em cada lista.</summary>
    public int LimiteLinhas { get; set; } = 8;
}
