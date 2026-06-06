namespace ProjectManagerWebAPI.Models;

public class Timesheet
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public required string Status { get; set; } = "Draft";
    public int? CreatedById { get; set; }
    public int? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Project? Project { get; set; }
    public virtual User? User { get; set; }
    public virtual User? CreatedBy { get; set; }
    public virtual User? ApprovedBy { get; set; }
    public virtual ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
}
