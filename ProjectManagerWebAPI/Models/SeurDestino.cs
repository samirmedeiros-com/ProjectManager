namespace ProjectManagerWebAPI.Models;

public class SeurDestino
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
    public string Apagado { get; set; } = "N";
}
