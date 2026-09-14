namespace ProjectManagerWebAPI.Models.TracePush;

/// <summary>
/// Estado de um push de trace, tal como o ecrã o lê.
///
/// Na tabela o estado é uma letra em CHRONO_WEB.CW_TRACEPUSH.FLAG, e são cinco e não três:
/// Y enviado, N por enviar, e <b>E, X e Z são todos erro</b> — cada um do lado de um cliente
/// diferente (E genérico, X quando o cliente devolve uma página HTML em vez da resposta,
/// Z quando devolve um SOAP fault). Agrupá-los é o que faz o total dos cartões bater certo
/// com o número de linhas; a letra continua a aparecer em cada linha para quem precisa dela.
/// </summary>
public enum EstadoPush
{
    Pendente,
    Enviado,
    Erro,
}

public static class EstadoPushMapa
{
    /// <summary>A letra da tabela → o estado do ecrã. Nulo e desconhecido contam como pendente e erro.</summary>
    public static EstadoPush DaFlag(string? flag) => (flag ?? "").Trim().ToUpperInvariant() switch
    {
        "Y" => EstadoPush.Enviado,
        "N" or "" => EstadoPush.Pendente,
        _ => EstadoPush.Erro,
    };

    /// <summary>As letras que compõem cada estado, para o WHERE do SQL.</summary>
    public static string? FiltroSql(string? estado) => (estado ?? "").Trim().ToLowerInvariant() switch
    {
        "enviado" => "NVL(t.FLAG,'N') = 'Y'",
        "pendente" => "NVL(t.FLAG,'N') = 'N'",
        "erro" => "NVL(t.FLAG,'N') NOT IN ('Y','N')",
        _ => null,
    };
}

/// <summary>Uma linha da lista. Sem REQUEST/RESPONSE: esses só no detalhe (são CLOB e VARCHAR2(4000)).</summary>
public class PushResumo
{
    /// <summary>Chave única da linha (índice CW_IDX_TRACEPUSH_HHPROWID). É o que se reenvia.</summary>
    public string HhpRowId { get; set; } = "";

    /// <summary>Número de guia (coluna ID) — o mesmo formato do HHPIDENTI do tracing.</summary>
    public string? Guia { get; set; }

    public string? Lote { get; set; }
    public string? UserLogin { get; set; }
    public string? Conta { get; set; }
    public string? Referencia { get; set; }
    public string? Codigo { get; set; }
    public string? Descricao { get; set; }
    public string? Flag { get; set; }
    public string Estado { get; set; } = "";
    public DateTime? Criado { get; set; }
    public DateTime? Processado { get; set; }
    public string? Destino { get; set; }
}

/// <summary>O detalhe do popup: tudo o que a lista não mostra, incluindo pedido e resposta.</summary>
public class PushDetalhe : PushResumo
{
    public string? Pedido { get; set; }
    public string? Resposta { get; set; }
    public string? Utilizador { get; set; }
    public string? DataEvento { get; set; }
    public string? Observacoes { get; set; }
    public string? Motorista { get; set; }
    public string? Pudo { get; set; }
}

/// <summary>Os valores acumulados dos cartões — de um dia só.</summary>
public class PushEstatisticas
{
    public DateTime Dia { get; set; }
    public int Enviados { get; set; }
    public int Erros { get; set; }
    public int Pendentes { get; set; }
    public int Total { get; set; }
}

/// <summary>Uma linha do report por userlogin.</summary>
public class PushPorUserLogin
{
    public string UserLogin { get; set; } = "";
    public int Enviados { get; set; }
    public int Erros { get; set; }
    public int Pendentes { get; set; }
    public int Total { get; set; }
}

/// <summary>Uma barra do gráfico: uma hora do dia escolhido.</summary>
public class PushPorHora
{
    public int Hora { get; set; }
    public int Enviados { get; set; }
    public int Erros { get; set; }
    public int Pendentes { get; set; }
    public int Total { get; set; }
}

/// <summary>
/// Pedido de reenvio. Não chama o webservice do cliente: repõe FLAG='N' e deixa o processo
/// automático de push fazer o envio real — o mesmo contrato da Gestão de Dados com as contas.
/// </summary>
public class PedidoReenvio
{
    public List<string> HhpRowIds { get; set; } = [];
}

public class ResultadoReenvio
{
    public int Pedidos { get; set; }
    public int Repostos { get; set; }
    public List<string> NaoEncontrados { get; set; } = [];
}

/// <summary>Uma linha de DPDIT.TRACEPUSH_REENVIO_LOG.</summary>
public class ReenvioLog
{
    public long Id { get; set; }
    public string? Utilizador { get; set; }
    public string HhpRowId { get; set; } = "";
    public string? Guia { get; set; }
    public string? UserLogin { get; set; }
    public string? Conta { get; set; }
    public string? FlagAnterior { get; set; }
    public bool Sucesso { get; set; }
    public string? Mensagem { get; set; }
    public DateTime CriadoEm { get; set; }
}
