namespace ProjectManagerWebAPI.Models;

public class TimesheetEntry
{
    public int Id { get; set; }
    public int TimesheetId { get; set; }
    public int DayOfWeek { get; set; }
    public decimal WorkHours { get; set; }
    public string? Notes { get; set; }

    public virtual Timesheet? Timesheet { get; set; }
}
