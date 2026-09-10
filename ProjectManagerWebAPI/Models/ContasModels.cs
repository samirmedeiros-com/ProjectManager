namespace ProjectManagerWebAPI.Models.Contas;

/// <summary>
/// Liga uma propriedade à coluna correspondente em WSDPD. As tabelas vêm do AS400 e têm
/// nomes que não sobrevivem a uma convenção automática (EEENT, BICSCCC2, SE_INFLIGHT_LOJAS_LIM_P),
/// por isso a correspondência é declarada aqui e o repositório constrói o SQL a partir dela —
/// em vez de repetir sessenta nomes de coluna no SELECT, no INSERT e no UPDATE.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ColunaAttribute(string nome) : Attribute
{
    public string Nome { get; } = nome;

    /// <summary>Chave técnica: nunca entra no UPDATE e é opcional no INSERT.</summary>
    public bool Chave { get; init; }
}

/// <summary>Linha de WSDPD.AS400_CONTAS.</summary>
public class Conta
{
    [Coluna("IDT", Chave = true)] public decimal? Idt { get; set; }

    [Coluna("EEENT")] public string? Eeent { get; set; }
    [Coluna("EENOM")] public string? Eenom { get; set; }
    [Coluna("EENOM2")] public string? Eenom2 { get; set; }
    [Coluna("EECTB")] public string? Eectb { get; set; }
    [Coluna("EECAE")] public string? Eecae { get; set; }
    [Coluna("FTSIT")] public string? Ftsit { get; set; }
    [Coluna("FTSGM")] public string? Ftsgm { get; set; }
    [Coluna("FTZON")] public string? Ftzon { get; set; }
    [Coluna("FTVEN")] public string? Ftven { get; set; }

    /// <summary>
    /// Estado de envio ao portal: N por enviar, Y enviada, E erro no último envio.
    /// Gravar uma conta repõe N — uma alteração que não volte a sair daqui não serve de nada.
    /// </summary>
    [Coluna("FLAGPORTAL")] public string? Flagportal { get; set; }
}

/// <summary>Linha de WSDPD.AS400_SUBCONTAS.</summary>
public class SubConta
{
    [Coluna("IDT", Chave = true)] public decimal? Idt { get; set; }

    [Coluna("EMENT")] public string? Ement { get; set; }
    [Coluna("EMEND")] public string? Emend { get; set; }
    [Coluna("EMDSC")] public string? Emdsc { get; set; }
    [Coluna("EMMOR")] public string? Emmor { get; set; }
    [Coluna("EMLOC")] public string? Emloc { get; set; }
    [Coluna("EMPOS")] public string? Empos { get; set; }
    [Coluna("EMPAI")] public string? Empai { get; set; }
    [Coluna("EMENF")] public string? Emenf { get; set; }
    [Coluna("EMTFT")] public string? Emtft { get; set; }
    [Coluna("EMEXP")] public string? Emexp { get; set; }
    [Coluna("BICACT")] public string? Bicact { get; set; }
    [Coluna("EMTRA")] public string? Emtra { get; set; }
    [Coluna("TELEFONE")] public string? Telefone { get; set; }
    [Coluna("TELEMOVEL")] public string? Telemovel { get; set; }
    [Coluna("EMAIL")] public string? Email { get; set; }
    [Coluna("SERVICE_RC")] public string? ServiceRc { get; set; }
    [Coluna("SERVICEB2C")] public string? ServiceB2c { get; set; }
    [Coluna("SERV_FRIO")] public string? ServFrio { get; set; }
    [Coluna("BICSCI")] public string? Bicsci { get; set; }
    [Coluna("BICSNIF")] public string? Bicsnif { get; set; }
    [Coluna("BICSCCC")] public string? Bicsccc { get; set; }
    [Coluna("BICSPRACA")] public string? Bicspraca { get; set; }
    [Coluna("BICSCCC2")] public string? Bicsccc2 { get; set; }
    [Coluna("BICSUSR2")] public string? Bicsusr2 { get; set; }
    [Coluna("BICSSERV")] public string? Bicsserv { get; set; }
    [Coluna("BICSPROD")] public string? Bicsprod { get; set; }
    [Coluna("BICVCLI")] public decimal? Bicvcli { get; set; }
    [Coluna("ACTIVE")] public string? Active { get; set; }
    [Coluna("GRASSINADA")] public string? Grassinada { get; set; }
    [Coluna("PREDICT")] public string? Predict { get; set; }
    [Coluna("MULTIPARCELA")] public string? Multiparcela { get; set; }
    [Coluna("COD")] public string? Cod { get; set; }
    [Coluna("SERVICO_RC_COM_COD")] public string? ServicoRcComCod { get; set; }
    [Coluna("TEXTO_SERVICO")] public string? TextoServico { get; set; }
    [Coluna("TEMPLATE_ETIQUETA")] public string? TemplateEtiqueta { get; set; }
    [Coluna("TEXT_ETIQUETA_AUXILIAR")] public string? TextEtiquetaAuxiliar { get; set; }
    [Coluna("TEMPLATE_ETIQUETA_COM_COD")] public string? TemplateEtiquetaComCod { get; set; }
    [Coluna("TEXT_ETIQUETA_COM_COD")] public string? TextEtiquetaComCod { get; set; }
    [Coluna("REEN_AUTO_LOJA")] public string? ReenAutoLoja { get; set; }
    [Coluna("LOJA_A_MOSTRAR")] public string? LojaAMostrar { get; set; }
    [Coluna("PICKUP_EXPEDITOR")] public string? PickupExpeditor { get; set; }
    [Coluna("PICKUP_DESTINATARIO")] public string? PickupDestinatario { get; set; }
    [Coluna("MORADA_DESTINATARIO")] public string? MoradaDestinatario { get; set; }
    [Coluna("LIMITE_PESO")] public string? LimitePeso { get; set; }
    [Coluna("PESO")] public string? Peso { get; set; }
    [Coluna("INFLIGHT_LOJAS")] public string? InflightLojas { get; set; }
    [Coluna("SE_INFLIGHT_LOJAS_LIM_VOL")] public string? SeInflightLojasLimVol { get; set; }
    [Coluna("SE_INFLIGHT_LOJAS_LIM_P")] public string? SeInflightLojasLimP { get; set; }

    /// <summary>Sim, "INFIGHT": a coluna está assim escrita na tabela e não pode ser corrigida aqui.</summary>
    [Coluna("INFIGHT_DATA")] public string? InfightData { get; set; }

    [Coluna("INFLIGHT_MORADA")] public string? InflightMorada { get; set; }
    [Coluna("SWAP")] public string? Swap { get; set; }
    [Coluna("TIPO_DESTINO")] public string? TipoDestino { get; set; }
    [Coluna("DIMENSOES")] public string? Dimensoes { get; set; }
    [Coluna("MEDIDAS")] public string? Medidas { get; set; }
    [Coluna("LOCKREPPIC")] public string? Lockreppic { get; set; }
    [Coluna("RECPRTDPDC")] public string? Recprtdpdc { get; set; }
    [Coluna("DESC_SERVICO")] public string? DescServico { get; set; }
    [Coluna("FRESH")] public string? Fresh { get; set; }
    [Coluna("SERVICE_FRIO")] public string? ServiceFrio { get; set; }
    [Coluna("FLAGPORTAL")] public string? Flagportal { get; set; }
}

/// <summary>Uma página de resultados com o total para a navegação.</summary>
public class Pagina<T>
{
    public List<T> Itens { get; set; } = [];
    public int Total { get; set; }
    public int PaginaAtual { get; set; }
    public int Tamanho { get; set; }
}

/// <summary>Linha da lista de contas: o que chega ao ecrã antes de abrir o detalhe.</summary>
public class ContaResumo
{
    public decimal? Idt { get; set; }
    public string? Eeent { get; set; }
    public string? Eenom { get; set; }
    public string? Ftsit { get; set; }
    public string? Flagportal { get; set; }
    public int SubContas { get; set; }
}

public class PedidoEnvio
{
    /// <summary>PRD ou QUA. O ambiente é sempre escolhido à mão — não há envio implícito aos dois.</summary>
    public string Ambiente { get; set; } = "";

    /// <summary>Número da conta (EEENT). Obrigatório.</summary>
    public string Conta { get; set; } = "";

    /// <summary>
    /// Subconta (EMEND) a enviar. Vazio envia a conta e todas as subcontas dela;
    /// preenchido envia só a conta e essa subconta.
    /// </summary>
    public string? SubConta { get; set; }
}

/// <summary>Resultado de um envio, um item por objeto enviado.</summary>
public class LinhaEnvio
{
    public string Tipo { get; set; } = "";
    public string Identificacao { get; set; } = "";

    /// <summary>PUT ou POST: o portal só aceita criar depois de a atualização falhar.</summary>
    public string Metodo { get; set; } = "";

    public int StatusCode { get; set; }
    public bool Sucesso { get; set; }
    public string? Resposta { get; set; }
}

public class ResultadoEnvio
{
    public string Ambiente { get; set; } = "";
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = "";
    public List<LinhaEnvio> Linhas { get; set; } = [];
}

/// <summary>Contagem por estado de envio de uma tabela (contas ou subcontas).</summary>
public class EstatisticaTabela
{
    /// <summary>FLAGPORTAL = 'Y'.</summary>
    public int Transmitidas { get; set; }
    /// <summary>FLAGPORTAL = 'N'.</summary>
    public int ATransmitir { get; set; }
    /// <summary>FLAGPORTAL = 'E'.</summary>
    public int Erros { get; set; }
    /// <summary>Qualquer outro valor de FLAGPORTAL (ou nulo): raro, mas não some da contagem.</summary>
    public int Outros { get; set; }
    public int Total { get; set; }
}

public class ContasEstatisticas
{
    public EstatisticaTabela Contas { get; set; } = new();
    public EstatisticaTabela SubContas { get; set; } = new();
}

/// <summary>Cabeçalho de um envio registado (DPDIT.CONTAS_ENVIO_LOG).</summary>
public class EnvioLog
{
    public long Id { get; set; }
    public string? Utilizador { get; set; }
    public string Ambiente { get; set; } = "";
    public string Conta { get; set; } = "";
    public string? SubConta { get; set; }
    public bool Sucesso { get; set; }
    public int TotalLinhas { get; set; }
    public int Falhas { get; set; }
    public string? Mensagem { get; set; }
    public long? ElapsedMs { get; set; }
    public DateTime CriadoEm { get; set; }
}

/// <summary>Resposta registada de uma conta/subconta (DPDIT.CONTAS_ENVIO_LOG_LINHA).</summary>
public class EnvioLogLinha
{
    public string Tipo { get; set; } = "";
    public string Identificacao { get; set; } = "";
    public string Metodo { get; set; } = "";
    public int StatusCode { get; set; }
    public bool Sucesso { get; set; }
    public string? Resposta { get; set; }
    public DateTime CriadoEm { get; set; }
}
