using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectManagerWebAPI.Models.Contas;

// Payloads aceites pelo portal de clientes (business.dpd.pt). Copiados do projeto
// ContasPortal_NET, que por sua vez espelha o JSON produzido pela aplicação Delphi
// original: o formato não é escolha nossa, é o que a API do outro lado já aceita.
// Mexer nos nomes ou nos defaults daqui parte envios que hoje funcionam.

public class AccountDto
{
    [JsonPropertyName("eeent")] public string Eeent { get; set; } = string.Empty;
    [JsonPropertyName("eenom")] public string Eenom { get; set; } = string.Empty;
    [JsonPropertyName("eenom2")] public string Eenom2 { get; set; } = string.Empty;
    [JsonPropertyName("eectb")] public string Eectb { get; set; } = string.Empty;
    [JsonPropertyName("eecae")] public string Eecae { get; set; } = string.Empty;
    [JsonPropertyName("ftsit")] public string Ftsit { get; set; } = string.Empty;
    [JsonPropertyName("ftsgm")] public string Ftsgm { get; set; } = string.Empty;
    [JsonPropertyName("ftzon")] public string Ftzon { get; set; } = string.Empty;
    [JsonPropertyName("ftven")] public string Ftven { get; set; } = string.Empty;
    [JsonPropertyName("active")] public bool Active { get; set; }

    /// <summary>
    /// Os defaults ("ND", "X", "0") não são cosmética: a API rejeita campos ausentes,
    /// e é assim que o processo automático os preenche. Manter igual.
    /// </summary>
    public static AccountDto DaConta(Conta c) => new()
    {
        Eeent = c.Eeent ?? string.Empty,
        Eenom = c.Eenom ?? string.Empty,
        Eenom2 = c.Eenom2 ?? "ND",
        Eectb = c.Eectb ?? string.Empty,
        Eecae = c.Eecae ?? "0",
        Ftsit = c.Ftsit ?? "X",
        Ftsgm = c.Ftsgm ?? "ND",
        Ftzon = c.Ftzon ?? "ND",
        Ftven = c.Ftven ?? "ND",
        Active = true
    };
}

public class SubAccountDto
{
    [JsonPropertyName("eeent")] public string Eeent { get; set; } = string.Empty;
    [JsonPropertyName("emend")] public string Emend { get; set; } = string.Empty;
    [JsonPropertyName("emdsc")] public string Emdsc { get; set; } = string.Empty;
    [JsonPropertyName("emmor")] public string Emmor { get; set; } = string.Empty;
    [JsonPropertyName("emloc")] public string Emloc { get; set; } = string.Empty;
    [JsonPropertyName("empos")] public string Empos { get; set; } = string.Empty;
    [JsonPropertyName("empai")] public string Empai { get; set; } = string.Empty;
    [JsonPropertyName("emenf")] public int Emenf { get; set; }
    [JsonPropertyName("emtft")] public string Emtft { get; set; } = string.Empty;
    [JsonPropertyName("emexp")] public string Emexp { get; set; } = string.Empty;
    [JsonPropertyName("emact")] public string Emact { get; set; } = string.Empty;
    [JsonPropertyName("emtra")] public string Emtra { get; set; } = string.Empty;
    [JsonPropertyName("telefone")] public string Telefone { get; set; } = "NA";
    [JsonPropertyName("email")] public string Email { get; set; } = "NA";
    [JsonPropertyName("service_rc")] public string ServiceRc { get; set; } = string.Empty;
    [JsonPropertyName("service_b2c")] public string ServiceB2c { get; set; } = string.Empty;
    [JsonPropertyName("service_frio")] public string ServiceFrio { get; set; } = string.Empty;
    [JsonPropertyName("bics_ci")] public string BicsCi { get; set; } = string.Empty;
    [JsonPropertyName("bics_nif")] public string BicsNif { get; set; } = string.Empty;
    [JsonPropertyName("bics_ccc")] public string BicsCcc { get; set; } = string.Empty;
    [JsonPropertyName("bics_praca")] public string BicsPraca { get; set; } = string.Empty;
    [JsonPropertyName("bics_ccc2")] public string BicsCcc2 { get; set; } = string.Empty;
    [JsonPropertyName("bics_usr2")] public string BicsUsr2 { get; set; } = string.Empty;
    [JsonPropertyName("bics_serv")] public string BicsServ { get; set; } = string.Empty;
    [JsonPropertyName("bics_prod")] public string BicsProd { get; set; } = string.Empty;
    [JsonPropertyName("bics_vcli")] public string BicsVcli { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("dpd_fresh_expire_min")] public int? DpdFreshExpireMin { get; set; } = 1;

    [JsonPropertyName("fitsitend")] public string Fitsitend { get; set; } = "NA";
    [JsonPropertyName("active")] public bool Active { get; set; }
    [JsonPropertyName("config")] public SubConfigDto Config { get; set; } = new();

    public static SubAccountDto DaSubConta(SubContaEnvio c)
    {
        static string NaStr(string? s) => string.IsNullOrWhiteSpace(s) ? "0" : s.Trim();
        static int NaInt(string? s) => int.TryParse((s ?? string.Empty).Trim(), out var n) ? n : 0;
        static string Left(string s, int n) => s.Length <= n ? s : s[..n];
        static bool? YnBool(string? s)
        {
            var t = (s ?? string.Empty).Trim();
            if (t == "Y") return true;
            if (t == "N") return false;
            return null;
        }

        var servFrioChar = Left(NaStr(c.ServFrio), 1);

        return new SubAccountDto
        {
            Eeent = NaStr(c.Ement),
            Emend = (int.TryParse((c.Emend ?? string.Empty).Trim(), out var em) ? em : 0).ToString("D2"),
            Emdsc = NaStr(c.Emdsc),
            Emmor = NaStr(Left(c.Emmor ?? string.Empty, 64)),
            Emloc = NaStr(c.Emloc),
            Empos = NaStr(c.Empos),
            Empai = NaStr(c.Empai),
            Emenf = NaInt(c.Emenf),
            Emtft = Left(NaStr(c.Emtft), 1),
            Emexp = NaStr(c.Emexp),
            Emact = NaStr(c.Bicact),
            Emtra = Left(NaStr(c.Emtra), 1),
            ServiceRc = NaStr(c.ServiceRc),
            ServiceB2c = Left(NaStr(c.ServiceB2c), 1),
            ServiceFrio = servFrioChar,
            BicsCi = NaStr(c.Bicsci),
            BicsNif = NaStr(c.Bicsnif),
            BicsCcc = NaStr(c.Bicsccc),
            BicsPraca = NaStr(c.Bicspraca),
            BicsCcc2 = NaStr(c.Bicsccc2),
            BicsUsr2 = NaStr(c.Bicsusr2),
            BicsServ = NaStr(c.Bicsserv),
            BicsProd = NaStr(c.Bicsprod),
            BicsVcli = (c.Bicvcli?.ToString() ?? string.Empty).Trim(),
            DpdFreshExpireMin = servFrioChar == "1" ? Math.Max(1, NaInt(c.Fresh)) : (int?)null,
            Active = (c.Active ?? string.Empty).Trim() == "true",
            Config = new SubConfigDto
            {
                Grassinada = YnBool(c.Grassinada),
                Predict = (c.Predict ?? string.Empty).Trim() == "Y" ? 1 : 0,
                Multiparcela = YnBool(c.Multiparcela),
                Cod = YnBool(c.Cod),
                ServicoRcComCod = new NullableJsonString(string.IsNullOrWhiteSpace(c.ServicoRcComCod) ? null : c.ServicoRcComCod.Trim()),
                TextoServico = (c.TextoServico ?? string.Empty).Trim(),
                TemplateEtiqueta = NaInt(c.TemplateEtiqueta),
                TextEtiquetaAuxiliar = (c.TextEtiquetaAuxiliar ?? string.Empty).Trim(),
                DescServico = (c.DescServico ?? string.Empty).Trim(),
                TemplateEtiquetaComCod = new NullableJsonInt(string.IsNullOrWhiteSpace(c.TemplateEtiquetaComCod) ? (int?)null : 1),
                EtiquetaImpressaPelaDpd = (c.PrtLabelDpd ?? string.Empty).Trim() == "1" ? 1 : 0,
                TextoEtiquetaComCod = string.IsNullOrWhiteSpace(c.TextEtiquetaComCod) ? "Y" : c.TextEtiquetaComCod.Trim(),
                ReencaminhamentoAutomaticoLoja = YnBool(c.ReenAutoLoja),
                LojaAMostrar = NaInt(c.LojaAMostrar),
                PickupExpeditor = YnBool(c.PickupExpeditor),
                PickupDestinatario = YnBool(c.PickupDestinatario),
                MoradaDestinatario = YnBool(c.MoradaDestinatario),
                LimitePeso = YnBool(c.LimitePeso),
                Peso = c.PesoNumerico.HasValue ? Convert.ToInt32(c.PesoNumerico.Value) : 0,
                SeInflightLojasLimiteVolumes = (c.SeInflightLojasLimVol ?? string.Empty).Trim(),
                SeInflightLojasLimitePeso = (c.SeInflightLojasLimP ?? string.Empty).Trim(),
                InflightData = YnBool(c.InfightData),
                InflightMorada = YnBool(c.InflightMorada),
                InflightLojas = false,
                Swap = YnBool(c.Swap),
                TipoDestino = (c.TipoDestino ?? string.Empty).Trim(),
                Dimensoes = string.IsNullOrWhiteSpace(c.Dimensoes) ? 0 : NaInt(c.Dimensoes),
                Medidas = (c.Medidas ?? string.Empty).Trim(),
            }
        };
    }
}

public class SubConfigDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("grassinada")] public bool? Grassinada { get; set; }

    [JsonPropertyName("predict")] public int Predict { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("multiparcela")] public bool? Multiparcela { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("cod")] public bool? Cod { get; set; }

    [JsonPropertyName("servico_rc_com_cod")] public NullableJsonString ServicoRcComCod { get; set; }
    [JsonPropertyName("texto_servico")] public string TextoServico { get; set; } = string.Empty;
    [JsonPropertyName("template_etiqueta")] public int TemplateEtiqueta { get; set; }
    [JsonPropertyName("text_etiqueta_auxiliar")] public string TextEtiquetaAuxiliar { get; set; } = string.Empty;
    [JsonPropertyName("desc_servico")] public string DescServico { get; set; } = string.Empty;
    [JsonPropertyName("template_etiqueta_com_cod")] public NullableJsonInt TemplateEtiquetaComCod { get; set; }
    [JsonPropertyName("etiqueta_impressa_pela_dpd")] public int EtiquetaImpressaPelaDpd { get; set; }
    [JsonPropertyName("texto_etiqueta_com_cod")] public string TextoEtiquetaComCod { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("reencaminhamento_automatico_loja")] public bool? ReencaminhamentoAutomaticoLoja { get; set; }

    [JsonPropertyName("loja_a_mostrar")] public int LojaAMostrar { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("pickup_expeditor")] public bool? PickupExpeditor { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("pickup_destinatario")] public bool? PickupDestinatario { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("morada_destinatario")] public bool? MoradaDestinatario { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("limite_peso")] public bool? LimitePeso { get; set; }

    [JsonPropertyName("peso")] public int Peso { get; set; }
    [JsonPropertyName("se_inflight_lojas_limite_volumes")] public string SeInflightLojasLimiteVolumes { get; set; } = string.Empty;
    [JsonPropertyName("se_inflight_lojas_limite_peso")] public string SeInflightLojasLimitePeso { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("inflight_data")] public bool? InflightData { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("inflight_morada")] public bool? InflightMorada { get; set; }

    [JsonPropertyName("inflight_lojas")] public bool InflightLojas { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("swap")] public bool? Swap { get; set; }

    [JsonPropertyName("tipo_destino")] public string TipoDestino { get; set; } = string.Empty;
    [JsonPropertyName("dimensoes")] public int Dimensoes { get; set; }
    [JsonPropertyName("medidas")] public string Medidas { get; set; } = string.Empty;
}

// Campos que a API exige presentes mesmo quando nulos (o Delphi envia TJSONNull).
// O serializador omite propriedades nulas, por isso usamos um tipo de valor (struct):
// nunca é "null" para o System.Text.Json — a chave sai sempre — e o conversor escreve
// um null explícito quando não há valor.
[JsonConverter(typeof(NullableJsonStringConverter))]
public readonly struct NullableJsonString(string? value)
{
    public string? Value { get; } = value;
}

public class NullableJsonStringConverter : JsonConverter<NullableJsonString>
{
    public override NullableJsonString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.TokenType == JsonTokenType.Null ? null : reader.GetString());

    public override void Write(Utf8JsonWriter writer, NullableJsonString value, JsonSerializerOptions options)
    {
        if (value.Value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value.Value);
    }
}

[JsonConverter(typeof(NullableJsonIntConverter))]
public readonly struct NullableJsonInt(int? value)
{
    public int? Value { get; } = value;
}

public class NullableJsonIntConverter : JsonConverter<NullableJsonInt>
{
    public override NullableJsonInt Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32());

    public override void Write(Utf8JsonWriter writer, NullableJsonInt value, JsonSerializerOptions options)
    {
        if (value.Value is null) writer.WriteNullValue();
        else writer.WriteNumberValue(value.Value.Value);
    }
}

/// <summary>
/// Subconta tal como sai da consulta de envio: além das colunas da tabela traz três valores
/// já resolvidos por joins — o serviço traduzido por WSDPD.CONTAS_FROMTO e o peso e o tipo de
/// destino sobrepostos por WSDPD.ACCOUNT_ALTERNATE_CONFIG. Enviar sem estes joins mandaria ao
/// portal um serviço que já não existe.
/// </summary>
public class SubContaEnvio : SubConta
{
    /// <summary>PESO já convertido a número (a coluna é texto e pode vir do config alternativo).</summary>
    public decimal? PesoNumerico { get; set; }

    /// <summary>NVL(AL.PRT_LABEL_DPD,'0') — não é coluna de AS400_SUBCONTAS.</summary>
    public string? PrtLabelDpd { get; set; }
}

public class tokenModel
{
    public string token_type { get; set; } = "";
    public int expires_in { get; set; }
    public string access_token { get; set; } = "";
}
