namespace ProjectManagerWebAPI.Models;

public class SeurVerify
{
    public long Idt { get; set; }
    public string? Guia { get; set; }
    public string? Inc { get; set; }
    public string? Dat { get; set; }
    public string? Hor { get; set; }
    public DateTime? DatahoraInsert { get; set; }
    public string? VerifyFlag { get; set; }
    public string? VerifyResposta { get; set; }
    public DateTime? VerifyData { get; set; }
}
