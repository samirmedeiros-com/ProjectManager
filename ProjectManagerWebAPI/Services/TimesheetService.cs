using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;
using ProjectManagerWebAPI.Controllers;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagerWebAPI.Services;

public class TimesheetService
{
    private readonly ApplicationDbContext _context;

    public TimesheetService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Timesheet> CreateTimesheetAsync(int projectId, int userId, DateTime weekStartDate, int createdById)
    {
        var weekEndDate = weekStartDate.AddDays(6);

        var existingTimesheet = await _context.Timesheets.FirstOrDefaultAsync(t =>
            t.ProjectId == projectId &&
            t.UserId == userId &&
            t.WeekStartDate == weekStartDate) != null;

        if (existingTimesheet)
            throw new InvalidOperationException("Timesheet já existe para este período");

        var timesheet = new Timesheet
        {
            ProjectId = projectId,
            UserId = userId,
            WeekStartDate = weekStartDate,
            WeekEndDate = weekEndDate,
            Status = "Draft",
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };

        _context.Timesheets.Add(timesheet);
        await _context.SaveChangesAsync();

        return timesheet;
    }

    public async Task<Timesheet?> GetTimesheetByIdAsync(int id)
    {
        return await _context.Timesheets
            .Include(t => t.Project)
            .Include(t => t.User)
            .Include(t => t.Entries)
            .Include(t => t.ApprovedBy)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Timesheet>> GetUserTimesheetsAsync(int userId)
    {
        return await _context.Timesheets
            .Where(t => t.UserId == userId)
            .Include(t => t.Project)
            .Include(t => t.Entries)
            .OrderByDescending(t => t.WeekStartDate)
            .ToListAsync();
    }

    public async Task<List<Timesheet>> GetPendingApprovalsAsync(int setorId)
    {
        return await _context.Timesheets
            .Where(t => t.Status == "Submitted" && t.Project != null && t.Project.SetorId == setorId)
            .Include(t => t.Project)
            .Include(t => t.User)
            .Include(t => t.Entries)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Timesheet> UpdateEntriesAsync(int id, List<Controllers.TimesheetEntryRequest> entries)
    {
        var timesheet = await GetTimesheetByIdAsync(id);
        if (timesheet == null)
            throw new KeyNotFoundException("Timesheet não encontrado");

        if (timesheet.Status != "Draft")
            throw new InvalidOperationException("Apenas timesheets em Draft podem ser editados");

        _context.TimesheetEntries.RemoveRange(timesheet.Entries);

        var totalHours = 0m;
        foreach (var entry in entries)
        {
            if (entry.WorkHours < 0 || entry.WorkHours > 12)
                throw new InvalidOperationException("Horas devem estar entre 0 e 12");

            totalHours += entry.WorkHours;

            var timesheetEntry = new TimesheetEntry
            {
                TimesheetId = id,
                DayOfWeek = entry.DayOfWeek,
                WorkHours = entry.WorkHours,
                Notes = entry.Notes
            };

            _context.TimesheetEntries.Add(timesheetEntry);
        }

        if (totalHours > 60)
            throw new InvalidOperationException("Total de horas na semana não pode exceder 60");

        timesheet.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return timesheet;
    }

    public async Task<Timesheet> SubmitTimesheetAsync(int id)
    {
        var timesheet = await GetTimesheetByIdAsync(id);
        if (timesheet == null)
            throw new KeyNotFoundException("Timesheet não encontrado");

        if (timesheet.Status != "Draft")
            throw new InvalidOperationException("Apenas timesheets em Draft podem ser submetidos");

        var totalHours = timesheet.Entries.Sum(e => e.WorkHours);
        if (totalHours == 0)
            throw new InvalidOperationException("Timesheet deve ter pelo menos uma hora registrada");

        timesheet.Status = "Submitted";
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return timesheet;
    }

    public async Task<Timesheet> ApproveTimesheetAsync(int id, int approvedById)
    {
        var timesheet = await GetTimesheetByIdAsync(id);
        if (timesheet == null)
            throw new KeyNotFoundException("Timesheet não encontrado");

        if (timesheet.Status != "Submitted")
            throw new InvalidOperationException("Apenas timesheets submetidos podem ser aprovados");

        timesheet.Status = "Approved";
        timesheet.ApprovedById = approvedById;
        timesheet.ApprovedAt = DateTime.UtcNow;
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return timesheet;
    }

    public async Task<Timesheet> RejectTimesheetAsync(int id, string reason)
    {
        var timesheet = await GetTimesheetByIdAsync(id);
        if (timesheet == null)
            throw new KeyNotFoundException("Timesheet não encontrado");

        if (timesheet.Status != "Submitted")
            throw new InvalidOperationException("Apenas timesheets submetidos podem ser rejeitados");

        timesheet.Status = "Draft";
        timesheet.RejectionReason = reason;
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return timesheet;
    }

    public async Task<List<Timesheet>> GetTimesheetsByProjectAsync(int projectId)
    {
        return await _context.Timesheets
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.User)
            .Include(t => t.Entries)
            .OrderByDescending(t => t.WeekStartDate)
            .ToListAsync();
    }

    private TimesheetResponse MapToResponse(Timesheet timesheet)
    {
        return new TimesheetResponse
        {
            Id = timesheet.Id,
            ProjectId = timesheet.ProjectId,
            ProjectName = timesheet.Project?.Name,
            UserId = timesheet.UserId,
            UserName = timesheet.User?.FullName,
            WeekStartDate = timesheet.WeekStartDate,
            WeekEndDate = timesheet.WeekEndDate,
            Status = timesheet.Status,
            Entries = timesheet.Entries.Select(e => new TimesheetEntryResponse
            {
                DayOfWeek = e.DayOfWeek,
                WorkHours = e.WorkHours,
                Notes = e.Notes
            }).ToList(),
            TotalHours = timesheet.Entries.Sum(e => e.WorkHours),
            ApprovedByName = timesheet.ApprovedBy?.FullName,
            ApprovedAt = timesheet.ApprovedAt,
            RejectionReason = timesheet.RejectionReason,
            CreatedAt = timesheet.CreatedAt
        };
    }

    public TimesheetResponse GetTimesheetResponse(Timesheet timesheet)
    {
        return MapToResponse(timesheet);
    }

    public List<TimesheetListResponse> GetTimesheetListResponse(List<Timesheet> timesheets)
    {
        return timesheets.Select(t => new TimesheetListResponse
        {
            Id = t.Id,
            ProjectName = t.Project?.Name,
            UserName = t.User?.FullName,
            WeekStartDate = t.WeekStartDate,
            WeekEndDate = t.WeekEndDate,
            Status = t.Status,
            TotalHours = t.Entries.Sum(e => e.WorkHours),
            CreatedAt = t.CreatedAt
        }).ToList();
    }
}
