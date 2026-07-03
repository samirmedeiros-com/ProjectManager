namespace ProjectManagerWebAPI.DTOs;

// ---- CWENT_NUM ----
public class CwentNumDto
{
    public string Account { get; set; } = string.Empty;
    public decimal BicNumber { get; set; }
    public string? ShEmail2Dest { get; set; }
    public string? ShReady2Collect { get; set; }
    public string? Cp4Allow { get; set; }
    public string? ServiceRcSeqNumber { get; set; }
    public string? AgregateBilling { get; set; }
    public string? GrAssinada { get; set; }
    public string? Weigth0Allow { get; set; }
    public string? LabelType { get; set; }
    public string? ServiceB2c { get; set; }
    public string? ServFrio { get; set; }
    public string? ServAux { get; set; }
    public string? DescAux { get; set; }
    public string? BicsUser { get; set; }
    public string? BicsPwd { get; set; }
    public string? BicsCi { get; set; }
    public string? BicsNif { get; set; }
    public string? BicsCcc { get; set; }
    public string? BicsPraca { get; set; }
    public string? BicsCcc2 { get; set; }
    public string? BicsUsr2 { get; set; }
    public string? BicsServ { get; set; }
    public string? BicsProd { get; set; }
    public string? BicvPortal { get; set; }
    public string? BicAux1 { get; set; }
    public string? BicAux2 { get; set; }
    public string? BicAux3 { get; set; }
    public string? BicAux4 { get; set; }
}

public class SaveCwentNumDto
{
    public decimal BicNumber { get; set; }
    public string? ShEmail2Dest { get; set; }
    public string? ShReady2Collect { get; set; }
    public string? Cp4Allow { get; set; }
    public string? ServiceRcSeqNumber { get; set; }
    public string? AgregateBilling { get; set; }
    public string? GrAssinada { get; set; }
    public string? Weigth0Allow { get; set; }
    public string? LabelType { get; set; }
    public string? ServiceB2c { get; set; }
    public string? ServFrio { get; set; }
    public string? ServAux { get; set; }
    public string? DescAux { get; set; }
    public string? BicsUser { get; set; }
    public string? BicsPwd { get; set; }
    public string? BicsCi { get; set; }
    public string? BicsNif { get; set; }
    public string? BicsCcc { get; set; }
    public string? BicsPraca { get; set; }
    public string? BicsCcc2 { get; set; }
    public string? BicsUsr2 { get; set; }
    public string? BicsServ { get; set; }
    public string? BicsProd { get; set; }
    public string? BicvPortal { get; set; }
    public string? BicAux1 { get; set; }
    public string? BicAux2 { get; set; }
    public string? BicAux3 { get; set; }
    public string? BicAux4 { get; set; }
}

public class CreateCwentNumDto : SaveCwentNumDto
{
    public string Account { get; set; } = string.Empty;
}

// ---- CPOSTAL ----
public class SeurCpostalDto
{
    public long Idt { get; set; }
    public string Postcode { get; set; } = string.Empty;
    public string? Population { get; set; }
    public string? Country { get; set; }
    public string? DestFranchise { get; set; }
    public string? Plataform { get; set; }
    public DateTime DtCriacao { get; set; }
    public DateTime DtAlteracao { get; set; }
}

public class SaveCpostalDto
{
    public string Postcode { get; set; } = string.Empty;
    public string? Population { get; set; }
    public string? Country { get; set; }
    public string? DestFranchise { get; set; }
    public string? Plataform { get; set; }
}

// ---- DESTINOS ----
public class SeurDestinoDto
{
    public long Idt { get; set; }
    public string DestinoCode { get; set; } = string.Empty;
    public string? PlataformCode { get; set; }
    public string? ProductCode { get; set; }
    public string? ServiceCode { get; set; }
    public string? Destination { get; set; }
    public string? LoadLine { get; set; }
    public string? TransportLine { get; set; }
    public DateTime DtCriacao { get; set; }
    public DateTime DtAlteracao { get; set; }
}

public class SaveDestinoDto
{
    public string DestinoCode { get; set; } = string.Empty;
    public string? PlataformCode { get; set; }
    public string? ProductCode { get; set; }
    public string? ServiceCode { get; set; }
    public string? Destination { get; set; }
    public string? LoadLine { get; set; }
    public string? TransportLine { get; set; }
}

// ---- PRODUCT ----
public class SeurProductDto
{
    public long Idt { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string? Product { get; set; }
    public string? ShortName { get; set; }
    public DateTime DtCriacao { get; set; }
    public DateTime DtAlteracao { get; set; }
}

public class SaveProductDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string? Product { get; set; }
    public string? ShortName { get; set; }
}

// ---- SERVICE ----
public class SeurServiceDto
{
    public long Idt { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string? Service { get; set; }
    public string? ShortName { get; set; }
    public DateTime DtCriacao { get; set; }
    public DateTime DtAlteracao { get; set; }
}

public class SaveServiceDto
{
    public string ServiceCode { get; set; } = string.Empty;
    public string? Service { get; set; }
    public string? ShortName { get; set; }
}
