namespace ProjectManagerWebAPI.Models;

public class SeurCpostal
{
    public long Idt { get; set; }
    public string Postcode { get; set; } = string.Empty;
    public string? Population { get; set; }
    public string? Country { get; set; }
    public string? DestFranchise { get; set; }
    public string? Plataform { get; set; }
    public DateTime DtCriacao { get; set; }
    public DateTime DtAlteracao { get; set; }
    public string Apagado { get; set; } = "N";
}
