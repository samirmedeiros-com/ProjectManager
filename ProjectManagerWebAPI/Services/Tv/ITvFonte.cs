namespace ProjectManagerWebAPI.Services.Tv;

/// <summary>
/// Uma origem de dados do mural. Cada fonte é independente das outras: contribui
/// um bloco com a sua chave e nada sabe do resto do ecrã.
///
/// Acrescentar uma fonte nova é escrever uma classe destas e registá-la no
/// Program.cs — o agregador e o frontend não precisam de mudar.
/// </summary>
public interface ITvFonte
{
    /// <summary>Chave do bloco na resposta (ex.: "projetos", "seur").</summary>
    string Chave { get; }

    Task<object> ObterAsync(CancellationToken ct);
}
