namespace ProjectManagerWebAPI.Controllers;

public class CreateTimesheetRequest
{
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public DateTime WeekStartDate { get; set; }
}

public class TimesheetEntryRequest
{
    public int DayOfWeek { get; set; }
    public decimal WorkHours { get; set; }
    public string? Notes { get; set; }
}

public class UpdateTimesheetEntriesRequest
{
    public List<TimesheetEntryRequest> Entries { get; set; } = new();
}

public class SubmitTimesheetRequest
{
    public string? Observations { get; set; }
}

public class ApproveTimesheetRequest
{
    public string? Observations { get; set; }
}

public class RejectTimesheetRequest
{
    public required string Reason { get; set; }
}

public class TimesheetEntryResponse
{
    public int DayOfWeek { get; set; }
    public decimal WorkHours { get; set; }
    public string? Notes { get; set; }
}

public class TimesheetResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<TimesheetEntryResponse> Entries { get; set; } = new();
    public decimal TotalHours { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TimesheetListResponse
{
    public int Id { get; set; }
    public string? ProjectName { get; set; }
    public string? UserName { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public DateTime CreatedAt { get; set; }
}
