namespace ProjectManagerWebAPI.Models;

public class SeurProduct
{
    public long Idt { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string? Product { get; set; }
    public string? ShortName { get; set; }
    public DateTime DtCriacao { get; set; }
    public DateTime DtAlteracao { get; set; }
    public string Apagado { get; set; } = "N";
}
