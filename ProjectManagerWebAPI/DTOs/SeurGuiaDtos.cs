namespace ProjectManagerWebAPI.DTOs;

public class SeurGuiaListDto
{
    public long Idt { get; set; }
    public string? Guia { get; set; }
    public string? Referencia { get; set; }
    public long? QtdVolumes { get; set; }
    public string? ContaDpd { get; set; }
    public string? DestinoNome { get; set; }
    public string? DestinoLocalidade { get; set; }
    public string? DestinoPais { get; set; }
    public long? Peso { get; set; }
    public string? Product { get; set; }
    public string? Service { get; set; }
    public string? FlagAtlas { get; set; }
    public string? FlagAtlasDescricao { get; set; }
    public DateTime DtCriacao { get; set; }
    public DateTime DtAlteracao { get; set; }
    public string? ShipmentCode { get; set; }
    public string? CodeStatusAtlas { get; set; }
    public DateTime? DtAtlas { get; set; }
}

public class SeurGuiaDetailDto
{
    public long Idt { get; set; }
    public string? Guia { get; set; }
    public string? Referencia { get; set; }
    public long? QtdVolumes { get; set; }
    public string? ContaDpd { get; set; }
    public long? Peso { get; set; }
    public long? ValorCod { get; set; }

    // Origem
    public string? OrigemNome { get; set; }
    public string? OrigemMorada { get; set; }
    public string? OrigemMoradaComplemento { get; set; }
    public string? OrigemCodigoPostal { get; set; }
    public string? OrigemLocalidade { get; set; }
    public string? OrigemPais { get; set; }
    public string? OrigemTelefone { get; set; }
    public string? OrigemTelemovel { get; set; }
    public string? OrigemFax { get; set; }
    public string? OrigemEmail { get; set; }
    public string? OrigemContatoNome { get; set; }
    public string? OrigemContatoTelefone { get; set; }
    public string? OrigemContatoTelemovel { get; set; }
    public string? OrigemContatoEmail { get; set; }
    public string? OrigemPraca { get; set; }

    // Destino
    public string? DestinoNome { get; set; }
    public string? DestinoMorada { get; set; }
    public string? DestinoMoradaComplemento { get; set; }
    public string? DestinoCodigoPostal { get; set; }
    public string? DestinoLocalidade { get; set; }
    public string? DestinoPais { get; set; }
    public string? DestinoTelefone { get; set; }
    public string? DestinoTelemovel { get; set; }
    public string? DestinoFax { get; set; }
    public string? DestinoEmail { get; set; }
    public string? DestinoContatoNome { get; set; }
    public string? DestinoContatoTelefone { get; set; }
    public string? DestinoContatoTelemovel { get; set; }
    public string? DestinoContatoEmail { get; set; }
    public string? DestinoPraca { get; set; }
    public string? Destino { get; set; }

    // Envio
    public string? Obs { get; set; }
    public string? ObsAdd { get; set; }
    public string? ProdutoCodigo { get; set; }
    public string? ServicoCodigo { get; set; }
    public string? Product { get; set; }
    public string? Service { get; set; }
    public string? Cccc { get; set; }
    public string? CodCentro { get; set; }
    public string? TransportLine { get; set; }
    public string? Digito { get; set; }
    public long? IdRange { get; set; }
    public string? Range { get; set; }
    public long? UserId { get; set; }
    public string? ParcelNumber { get; set; }
    public string? GuiaDpd { get; set; }
    public string? ShipmentCode { get; set; }

    // Atlas
    public string? FlagAtlas { get; set; }
    public string? FlagAtlasDescricao { get; set; }
    public string? CodeStatusAtlas { get; set; }
    public DateTime? DtAtlas { get; set; }
    public string? RequestAtlas { get; set; }
    public string? RespAtlas { get; set; }

    // CIT
    public string? FlagCit { get; set; }
    public DateTime? DtCit { get; set; }
    public string? RespCit { get; set; }

    // Flags sistema
    public string? FlagAs400 { get; set; }
    public string? Apagado { get; set; }

    // Verify
    public string? VerifyFlag { get; set; }
    public string? VerifyResp { get; set; }
    public string? VerifyInc { get; set; }
    public string? VerifyDatahoraInc { get; set; }
    public DateTime? VerifyDatahoraUpd { get; set; }
    public DateTime? DataVerifyTrace { get; set; }
    public string? VerifyTrace { get; set; }

    public DateTime DtCriacao { get; set; }
    public DateTime DtAlteracao { get; set; }
}

public class UpdateGuiaDto
{
    public string? Guia { get; set; }
    public string? Referencia { get; set; }
    public long? QtdVolumes { get; set; }
    public string? ContaDpd { get; set; }
    public long? Peso { get; set; }
    public long? ValorCod { get; set; }

    // Origem
    public string? OrigemNome { get; set; }
    public string? OrigemMorada { get; set; }
    public string? OrigemMoradaComplemento { get; set; }
    public string? OrigemCodigoPostal { get; set; }
    public string? OrigemLocalidade { get; set; }
    public string? OrigemPais { get; set; }
    public string? OrigemTelefone { get; set; }
    public string? OrigemTelemovel { get; set; }
    public string? OrigemFax { get; set; }
    public string? OrigemEmail { get; set; }
    public string? OrigemContatoNome { get; set; }
    public string? OrigemContatoTelefone { get; set; }
    public string? OrigemContatoTelemovel { get; set; }
    public string? OrigemContatoEmail { get; set; }
    public string? OrigemPraca { get; set; }

    // Destino
    public string? DestinoNome { get; set; }
    public string? DestinoMorada { get; set; }
    public string? DestinoMoradaComplemento { get; set; }
    public string? DestinoCodigoPostal { get; set; }
    public string? DestinoLocalidade { get; set; }
    public string? DestinoPais { get; set; }
    public string? DestinoTelefone { get; set; }
    public string? DestinoTelemovel { get; set; }
    public string? DestinoFax { get; set; }
    public string? DestinoEmail { get; set; }
    public string? DestinoContatoNome { get; set; }
    public string? DestinoContatoTelefone { get; set; }
    public string? DestinoContatoTelemovel { get; set; }
    public string? DestinoContatoEmail { get; set; }
    public string? DestinoPraca { get; set; }
    public string? Destino { get; set; }

    // Envio
    public string? Obs { get; set; }
    public string? ObsAdd { get; set; }
    public string? ProdutoCodigo { get; set; }
    public string? ServicoCodigo { get; set; }
    public string? Product { get; set; }
    public string? Service { get; set; }
    public string? Cccc { get; set; }
    public string? CodCentro { get; set; }
    public string? TransportLine { get; set; }
    public string? Digito { get; set; }
    public long? IdRange { get; set; }
    public string? Range { get; set; }
    public long? UserId { get; set; }
    public string? ParcelNumber { get; set; }
    public string? GuiaDpd { get; set; }
    public string? ShipmentCode { get; set; }

    // Atlas
    public string? FlagAtlas { get; set; }
    public string? CodeStatusAtlas { get; set; }
    public DateTime? DtAtlas { get; set; }
    public string? RequestAtlas { get; set; }
    public string? RespAtlas { get; set; }

    // CIT
    public string? FlagCit { get; set; }
    public DateTime? DtCit { get; set; }
    public string? RespCit { get; set; }

    // Flags sistema
    public string? FlagAs400 { get; set; }
    public string? Apagado { get; set; }

    // Verify
    public string? VerifyFlag { get; set; }
    public string? VerifyResp { get; set; }
    public string? VerifyInc { get; set; }
    public string? VerifyDatahoraInc { get; set; }
    public DateTime? VerifyDatahoraUpd { get; set; }
    public DateTime? DataVerifyTrace { get; set; }
    public string? VerifyTrace { get; set; }
}

public class UpdateFlagAtlasDto
{
    public required string FlagAtlas { get; set; }
}

public class SeurTotaisDto
{
    public int Total { get; set; }
    public int Enviado { get; set; }
    public int NaoEnviado { get; set; }
    public int Erro { get; set; }
    public int Outros { get; set; }
}
