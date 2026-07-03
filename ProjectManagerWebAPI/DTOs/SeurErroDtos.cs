namespace ProjectManagerWebAPI.DTOs;

public class SeurErroDto
{
    public long Idt { get; set; }
    public string? Ecb { get; set; }
    public string? Referencia { get; set; }
    public string? Title { get; set; }
    public string? Status { get; set; }
    public string? Detail { get; set; }
    public DateTime? DatahoraInsert { get; set; }
}
