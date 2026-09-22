namespace ProjectManagerWebAPI.Services;

/// <summary>
/// Como um SHPNOT se lê no ecrã: que abas existem, que tabela dá cada bloco e por que coluna
/// se chega a ela. É a tradução do modelo do WebApiShpNot (~65 tabelas, quase todas ligadas
/// uma-a-uma) para as sete abas que uma pessoa consegue percorrer.
///
/// <para>Declara-se aqui e não no SQL de propósito. A view <c>VW_SHPNOT_AS400</c> junta tudo
/// num único SELECT achatado, e por isso <b>multiplica linhas</b>: um SHPNOT com 3 volumes,
/// 2 mensagens cada e 4 linhas de fatura sai de lá com dezenas de linhas iguais em quase
/// tudo. Serve para alimentar o AS400 e para saber o alias de cada campo — não para
/// mostrar. Cada bloco é lido à parte, pela sua chave.</para>
/// </summary>
public static class ShpNotEstrutura
{
    /// <summary>Como se chega de um bloco ao seguinte.</summary>
    public enum Ligacao
    {
        /// <summary>O pai guarda o ID do filho numa coluna (ligação um-a-um).</summary>
        PaiAponta,

        /// <summary>O filho guarda o ID do pai (coleção: volumes, mensagens, imagens).</summary>
        FilhoAponta,
    }

    public sealed record Bloco(
        string Titulo,
        string Tabela,
        Ligacao Ligacao,
        /// <summary>A coluna que faz a ligação, na tabela que a guarda.</summary>
        string Chave,
        Bloco[]? Filhos = null,
        /// <summary>Coluna que resume a linha quando o bloco é uma coleção fechada.</summary>
        string? Rotulo = null,
        /// <summary>
        /// Como se ordena a coleção. É uma expressão SQL e não só um nome de coluna porque
        /// os números destas tabelas estão guardados como texto: ordenar
        /// <c>PARCELRANK</c> tal e qual punha o volume 10 antes do 2.
        /// </summary>
        string? Ordem = null);

    public sealed record Aba(string Chave, string Titulo, Bloco[] Blocos);

    private static Bloco Um(string titulo, string tabela, string chave, Bloco[]? filhos = null)
        => new(titulo, tabela, Ligacao.PaiAponta, chave, filhos);

    private static Bloco Muitos(string titulo, string tabela, string chave, Bloco[]? filhos = null,
                                string? rotulo = null, string? ordem = null)
        => new(titulo, tabela, Ligacao.FilhoAponta, chave, filhos, rotulo, ordem);

    /// <summary>
    /// Ordem crescente por uma coluna de texto que guarda um número. O <c>lpad</c> alinha os
    /// algarismos à direita antes de comparar, que é o que faz 2 vir antes de 10; as linhas
    /// sem valor ficam no fim, e não à frente do volume 1.
    /// </summary>
    private static string Numerica(string coluna) => $"nvl2({coluna}, 0, 1), lpad(trim({coluna}), 12, '0')";

    private static Bloco Morada(string titulo, string tabela, string chave) => Um(titulo, tabela, chave);

    public static readonly Aba[] Abas =
    [
        new("envio", "Envio",
        [
            Um("Dados do envio", "SHIPMENTINFOS", "SHIPMENTINFOSID",
            [
                Um("Peso e dimensões", "WEIGHTANDDIMENSION", "WEIGHTANDDIMENSIONID",
                [
                    Um("Peso", "WEIGHT", "WEIGHTID"),
                    Um("Dimensões", "DIMENSIONS", "DIMENSIONSID"),
                ]),
            ]),
            Um("Encaminhamento", "ROUTINGINFOS", "ROUTINGINFOSID"),
            Um("Entrega", "DELIVERY", "DELIVERYID"),
        ]),

        new("remetente", "Remetente",
        [
            Um("Remetente", "SENDER", "SENDERID",
            [
                Um("Conta de cliente", "CUSTOMERINFOS", "CUSTOMERINFOSID"),
                Morada("Morada", "SENDERADDRESS", "SENDERADDRESSID"),
                Um("Contacto", "SENDERCONTACT", "SENDERCONTACTID"),
                Um("Coordenadas", "SENDERGPS", "SENDERGPSID"),
                Um("Entidade legal", "SENDERLEGALENTITY", "SENDERLEGALENTITYID"),
            ]),
        ]),

        new("destinatario", "Destinatário",
        [
            Um("Destinatário", "RECEIVER", "RECEIVERID",
            [
                Morada("Morada", "RECEIVERADDRESS", "RECEIVERADDRESSID"),
                Um("Contacto", "RECEIVERCONTACT", "RECEIVERCONTACTID"),
                Um("Coordenadas", "RECEIVERGPS", "RECEIVERGPSID"),
                Um("Entidade legal", "RECEIVERLEGALENTITY", "RECEIVERLEGALENTITYID"),
            ]),
            Um("Pessoa", "PERSON", "PERSONID",
            [
                Morada("Morada da pessoa", "PERSADDRESS", "PERSADDRESSID"),
                Um("Coordenadas da pessoa", "PERSGPS", "PERSGPSID"),
            ]),
        ]),

        new("volumes", "Volumes",
        [
            Muitos("Volumes", "PARCEL", "SHPNOTID",
            [
                Um("Identificação", "PARCELINFOS", "PARCELINFOSID",
                [
                    Um("Dimensões", "DIMENSIONS", "DIMENSIONSID"),
                    Um("Peso declarado", "DECLAREDWEIGHT", "DECLAREDWEIGHTID"),
                    Um("Peso medido", "MEASUREDWEIGHT", "MEASUREDWEIGHTID"),
                ]),
                Um("Serviços", "SERVICECODES", "SERVICECODESID"),
                Um("Seguro", "HINS", "HINSID", [Um("Valor seguro", "HINSAMOUNT", "HINSAMOUNTID")]),
                Um("Custo CIF", "CIFCOST", "CIFCOSTID"),
                Um("Cobrança (COD)", "COD", "CODID", [Um("Valor a cobrar", "NAMOUNT", "NAMOUNTID")]),
                Um("Encargos (ROD)", "ROD", "RODID",
                [
                    Um("Direitos", "DUTIES", "DUTIESID"),
                    Um("Impostos", "TAXES", "TAXESID"),
                    Um("Taxas aduaneiras", "CUSTOMSFEES", "CUSTOMSFEESID"),
                    Um("Extra", "EXTRA", "EXTRAID"),
                ]),
                Um("Troca", "SWAP", "SWAPID"),
                Um("Informação de expedição", "SHIPINFO", "SHIPINFOID"),
                Um("Devolução", "RETURNINFOS", "RETURNINFOSID",
                [
                    Morada("Morada de devolução", "RETADDRESS", "RETADDRESSID"),
                    Um("Contacto de devolução", "RETCONTACT", "RETCONTACTID"),
                    Um("Coordenadas de devolução", "RETGPS", "RETGPSID"),
                ]),
                Um("Mercadoria", "GOODS", "GOODSID", [Um("Temperatura", "TEMPERATURE", "TEMPERATUREID")]),
                Muitos("Notificações", "MESSAGE", "PARCELID", rotulo: "MESSAGETYPE"),
                Muitos("Matérias perigosas", "HAZARDOUSSUBSTANCE", "PARCELID",
                [
                    Um("Peso da substância", "SUBWEIGHT", "SUBWEIGHTID"),
                    Um("Peso explosivo", "EXPLWEIGHT", "EXPLWEIGHTID"),
                ], rotulo: "UNNO"),
            ], rotulo: "PARCELRANK", ordem: Numerica("PARCELRANK")),
        ]),

        new("internacional", "Internacional",
        [
            Um("Alfândega", "INTERNATIONAL", "INTERNATIONALID",
            [
                Um("Valor", "CAMOUNT", "CAMOUNTID"),
                Um("Valor (câmbio)", "CAMOUNTEX", "CAMOUNTEXID"),
                Morada("Morada aduaneira", "CADDRESS", "CADDRESSID"),
                Um("Contacto aduaneiro", "CCONTACT", "CCONTACTID"),
                Um("Coordenadas aduaneiras", "CGPS", "CGPSID"),
                Morada("Morada do importador", "SIADDRESS", "SIADDRESSID"),
                Um("Contacto do importador", "SICONTACT", "SICONTACTID"),
                Um("Coordenadas do importador", "SIGPS", "SIGPSID"),
                Muitos("Linhas de fatura", "INTERINVOICELINE", "INTERNATIONALID",
                [
                    Um("Peso líquido", "CNETWEIGHT", "CNETWEIGHTID"),
                    Um("Peso bruto", "CGROSSWEIGHT", "CGROSSWEIGHTID"),
                ], rotulo: "CINVOICEPOSITION", ordem: Numerica("CINVOICEPOSITION")),
                Muitos("Imagens", "IMAGE", "INTERNATIONALID", rotulo: "IMGCATEGORY",
                    ordem: "\"IMGDateTime\""),
            ]),
        ]),

        new("enriquecimento", "Enriquecimento",
        [
            Um("Enriquecimento", "ENRICHMENTS", "ENRICHMENTSID",
            [
                Um("Tipo de negócio", "BUSINESSTYPES", "BUSINESSTYPESID"),
                Morada("Morada corrigida", "RECEIVERADDRESSCORRECTED", "RECEIVERADDRESSCORRECTEDID"),
                Um("Correções aplicadas", "ADDRESSCORRECTIONJOURNEY", "ADDRESSCORRECTIONJOURNEYID"),
            ]),
        ]),
    ];

    /// <summary>
    /// Onde cada tabela da receção aparece no ecrã: a aba e o nome do bloco.
    ///
    /// <para>Serve os SHPNOTs que <b>enviamos</b>. Esses vivem numa tabela só, larga, de
    /// colunas com nomes de AS400 (<c>SNAME1X</c>, <c>RTOWNX</c>) — não têm estrutura própria
    /// para o ecrã seguir. Mas cada uma dessas colunas é o alias de um campo que, do lado da
    /// receção, tem tabela: o alias diz a tabela, a tabela diz a aba, e um envio enviado
    /// lê-se com a mesma arrumação de um envio recebido.</para>
    /// </summary>
    public static readonly Dictionary<string, (string Aba, string Bloco)> OndeVive = Mapear();

    private static Dictionary<string, (string, string)> Mapear()
    {
        var mapa = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["SHPNOTIN"] = ("envio", "Cabeçalho"),
        };

        void Percorrer(string aba, Bloco bloco)
        {
            mapa.TryAdd(bloco.Tabela, (aba, bloco.Titulo));
            foreach (var filho in bloco.Filhos ?? []) Percorrer(aba, filho);
        }

        foreach (var aba in Abas)
            foreach (var bloco in aba.Blocos)
                Percorrer(aba.Chave, bloco);

        return mapa;
    }

    /// <summary>Título de uma aba pela chave, para o ecrã dos SHPNOTs enviados.</summary>
    public static string TituloAba(string chave) =>
        Abas.FirstOrDefault(a => a.Chave == chave)?.Titulo ?? chave;

    /// <summary>
    /// Colunas que o ecrã não mostra: chaves técnicas, que não são dados do SHPNOT e enchem
    /// os blocos de identificadores sem significado para quem consulta.
    /// </summary>
    public static bool Escondida(string tabela, string coluna)
    {
        if (coluna.Equals("ID", StringComparison.OrdinalIgnoreCase)) return true;
        if (!coluna.EndsWith("ID", StringComparison.OrdinalIgnoreCase)) return false;

        // Nem todas as colunas terminadas em ID são chaves: estas são dados do envio.
        return coluna.ToUpperInvariant() switch
        {
            "MPSID" or "ORIGINMPSID" or "UNIQCUSTID" or "RECEIVERCUSTID" or "RECEIVERPUDOID"
                or "PERSID" or "UNIQUEID" or "RETPUDOID" or "IMGID" => false,
            _ => true,
        };
    }
}
