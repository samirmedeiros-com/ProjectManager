namespace ProjectManagerWebAPI.Models;

public class UserSetor
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SetorId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    public virtual Setor Setor { get; set; } = null!;
}
