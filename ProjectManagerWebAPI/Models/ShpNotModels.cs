namespace ProjectManagerWebAPI.Models.ShpNot;

/// <summary>
/// Um campo tal como o ecrã o mostra: o valor e as três identidades que o mesmo dado tem
/// consoante quem fala dele — o nome no JSON recebido, a coluna na base, e o alias com que
/// viaja para o AS400 na GROUPSHPNOT.VW_SHPNOT_AS400. As três vão para o hint.
/// </summary>
public sealed record CampoShpNot
{
    /// <summary>Etiqueta a mostrar. É o nome no JSON quando existe; senão a coluna.</summary>
    public required string Etiqueta { get; init; }

    /// <summary>Nome do campo no JSON do SHPNOT. Vazio nas colunas de controlo.</summary>
    public string? Json { get; init; }

    public required string Tabela { get; init; }
    public required string Coluna { get; init; }

    /// <summary>Alias na VW_SHPNOT_AS400. Vazio nos campos que a view não leva.</summary>
    public string? Alias { get; init; }

    /// <summary>
    /// O mesmo campo do outro lado. Nos SHPNOTs que enviamos, a coluna <b>é</b> o alias, e o
    /// que falta é saber onde o mesmo dado está guardado quando somos nós a receber — por
    /// exemplo <c>MPSIDX</c> aqui, <c>SHIPMENTINFOS.MPSID</c> lá.
    /// </summary>
    public string? Equivalente { get; init; }

    public string? Valor { get; init; }

    /// <summary>
    /// Verdadeiro nas colunas guardadas como texto JSON (listas convertidas pelo EF, ex.:
    /// SPARTNERREFS). O ecrã mostra-as como lista e não como uma cadeia de caracteres.
    /// </summary>
    public bool Lista { get; init; }
}

/// <summary>
/// Um bloco do ecrã. Ou é uma linha só — e então os valores estão em <see cref="Campos"/> —
/// ou é uma coleção (volumes, mensagens, imagens) e então vem em <see cref="Linhas"/>, que o
/// ecrã desenha como tabela. Em qualquer dos casos pode ter blocos filhos por baixo.
/// </summary>
public sealed record NoShpNot
{
    public required string Titulo { get; init; }
    public required string Tabela { get; init; }
    public List<CampoShpNot> Campos { get; init; } = [];
    public List<NoShpNot> Filhos { get; init; } = [];

    public bool Colecao { get; init; }
    public List<LinhaShpNot> Linhas { get; init; } = [];

    /// <summary>Vazio quando a linha não existe: o SHPNOT não trouxe este bloco.</summary>
    public bool Vazio { get; init; }
}

/// <summary>Uma linha de uma coleção, com os seus próprios blocos filhos.</summary>
public sealed record LinhaShpNot
{
    public required string Id { get; init; }
    /// <summary>O que resume a linha na tabela fechada (nº do volume, tipo de mensagem...).</summary>
    public string? Rotulo { get; init; }
    public List<CampoShpNot> Campos { get; init; } = [];
    public List<NoShpNot> Filhos { get; init; } = [];
}

/// <summary>Uma aba do ecrã, com os blocos que lhe pertencem.</summary>
public sealed record AbaShpNot
{
    public required string Chave { get; init; }
    public required string Titulo { get; init; }
    public List<NoShpNot> Blocos { get; init; } = [];
}

/// <summary>
/// De que lado está o SHPNOT: <c>entrada</c> é o que recebemos do Geopost e o WebApiShpNot
/// guardou; <c>saida</c> é o que nós enviamos, a partir das tabelas do AS400.
/// </summary>
public static class SentidoShpNot
{
    public const string Entrada = "entrada";
    public const string Saida = "saida";
}

/// <summary>O que a listagem mostra por SHPNOT, sem descer ao grafo todo.</summary>
public sealed record ShpNotResumo
{
    public string Sentido { get; init; } = SentidoShpNot.Entrada;

    public required long Idt { get; init; }
    public required string Id { get; init; }
    public string? MpsId { get; init; }
    public string? Remetente { get; init; }
    public string? Destinatario { get; init; }
    public string? Pais { get; init; }
    public int? Volumes { get; init; }
    public string? RespServ { get; init; }
    /// <summary>Y enviado ao AS400, N por enviar, E erro. A letra fica à vista.</summary>
    public string? FlagAs400 { get; init; }
    public string Estado { get; init; } = "";
    public DateTime? Recebido { get; init; }
    public DateTime? ProcessadoAs400 { get; init; }

    /// <summary>
    /// O que falhou, quando falhou. Só os SHPNOTs que enviamos o sabem dizer: a
    /// <c>GEODT01SPN</c> guarda a resposta do serviço em <c>RESPSERV</c> ("FALTA DADOS", o
    /// erro devolvido pelo Geopost). Do lado da receção não há texto nenhum — o
    /// IntegratorAS400 marca a letra E e a data, e mais nada, por isso aqui vem vazio.
    /// </summary>
    public string? Erro { get; init; }
}

public sealed record ShpNotDetalhe
{
    public required ShpNotResumo Resumo { get; init; }
    public List<AbaShpNot> Abas { get; init; } = [];
}

/// <summary>
/// Os cartões do topo: o estado da fila para o AS400, não um resumo do dia.
///
/// <para>É de propósito. Contar os SHPNOTs de um dia obriga a varrer a tabela inteira —
/// <c>DATAINSERT</c> não tem índice e entram ~80 mil envios por dia —, enquanto
/// <c>FLAGAS400</c> tem índice e os dois valores que interessam (por integrar e com erro)
/// são poucas centenas. Os números que aqui aparecem custam dois segundos; os do dia
/// custavam minutos e diziam menos.</para>
/// </summary>
public sealed record ShpNotEstatisticas
{
    public int Pendentes { get; init; }
    public int Erros { get; init; }
    /// <summary>Integrados no AS400 hoje. Ver a nota em <see cref="FilaSaida.SucessoHoje"/>.</summary>
    public int SucessoHoje { get; init; }
    /// <summary>O último SHPNOT que entrou — diz de relance se a receção está viva.</summary>
    public DateTime? UltimoRecebido { get; init; }
    public long? UltimoIdt { get; init; }

    /// <summary>
    /// A fila de saída: os SHPNOTs que temos para enviar ao Geopost, da GEODT01SPN.
    /// São três filas independentes sobre a mesma linha — o envio do SHPNOT, o depot
    /// scanning e o dispatcher —, cada uma com a sua letra e a sua data.
    /// </summary>
    public FilaSaida? Saida { get; init; }
}

public sealed record FilaSaida
{
    public int Pendentes { get; init; }
    public int Erros { get; init; }

    /// <summary>
    /// Entregues ao Geopost <b>hoje</b>, e não desde sempre. O acumulado de todos os tempos
    /// são 51 milhões de linhas e custa minuto e meio a contar; o do dia custa segundos e é
    /// o que diz se o envio está a correr agora.
    /// </summary>
    public int SucessoHoje { get; init; }
    public int PendentesScan { get; init; }
    public int PendentesDespacho { get; init; }
    public DateTime? UltimoInserido { get; init; }
}

/// <summary>
/// Uma fatia da listagem. Não traz total: sabê-lo obrigaria a contar tudo o que o filtro
/// apanha antes de mostrar as dez primeiras linhas, e é isso que torna a consulta lenta.
/// <see cref="HaMais"/> chega para paginar.
/// </summary>
public sealed record FatiaShpNot
{
    public List<ShpNotResumo> Itens { get; init; } = [];
    public int PaginaAtual { get; init; }
    public int Tamanho { get; init; }
    public bool HaMais { get; init; }
}

public sealed record FiltroShpNot(
    DateTime? Dia,
    string? MpsId,
    string? Volume,
    string? Estado,
    string? RespServ,
    int Pagina,
    int Tamanho,
    /// <summary>Vazio traz os dois lados; senão só o que recebemos ou só o que enviamos.</summary>
    string? Sentido = null);

