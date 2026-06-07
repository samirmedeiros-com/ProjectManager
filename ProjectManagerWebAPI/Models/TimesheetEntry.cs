namespace ProjectManagerWebAPI.Models;

public class TimesheetEntry
{
    public int Id { get; set; }
    public int TimesheetId { get; set; }
    public int ProjectId { get; set; }
    public decimal WorkHours { get; set; }
    public string? Notes { get; set; }

    public virtual Timesheet? Timesheet { get; set; }
    public virtual Project? Project { get; set; }
}
