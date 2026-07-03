namespace ProjectManagerWebAPI.Models;

public class SeurService
{
    public long Idt { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string? Service { get; set; }
    public string? ShortName { get; set; }
    public DateTime DtCriacao { get; set; }
    public DateTime DtAlteracao { get; set; }
    public string Apagado { get; set; } = "N";
}
