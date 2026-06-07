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

    public async Task<Timesheet> GetOrCreateDailyTimesheetAsync(int userId, DateTime date)
    {
        var dateOnly = date.Date;
        var timesheet = await _context.Timesheets
            .Include(t => t.Entries)
            .ThenInclude(e => e.Project)
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Date == dateOnly);

        if (timesheet == null)
        {
            timesheet = new Timesheet
            {
                UserId = userId,
                Date = dateOnly,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Timesheets.Add(timesheet);
            await _context.SaveChangesAsync();
        }

        return timesheet;
    }

    public async Task<Timesheet?> GetTimesheetByIdAsync(int id)
    {
        return await _context.Timesheets
            .Include(t => t.User)
            .Include(t => t.Entries)
            .ThenInclude(e => e.Project)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Timesheet>> GetUserTimesheetsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Timesheets
            .Where(t => t.UserId == userId)
            .Include(t => t.Entries)
            .ThenInclude(e => e.Project)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value.Date);

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }

    public async Task<Timesheet> AddProjectEntryAsync(int timesheetId, int projectId, decimal workHours, string? notes)
    {
        var timesheet = await GetTimesheetByIdAsync(timesheetId);
        if (timesheet == null)
            throw new KeyNotFoundException("Timesheet não encontrado");

        if (timesheet.Status != "Draft" && timesheet.Status != "Rejected")
            throw new InvalidOperationException("Só é possível adicionar horas em timesheets em Draft ou Rejeitadas");

        var existingEntry = timesheet.Entries.FirstOrDefault(e => e.ProjectId == projectId);
        if (existingEntry != null)
        {
            existingEntry.WorkHours = workHours;
            existingEntry.Notes = notes;
        }
        else
        {
            var entry = new TimesheetEntry
            {
                TimesheetId = timesheetId,
                ProjectId = projectId,
                WorkHours = workHours,
                Notes = notes
            };
            timesheet.Entries.Add(entry);
        }

        timesheet.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return timesheet;
    }

    public async Task<Timesheet> RemoveProjectEntryAsync(int timesheetId, int projectId)
    {
        var timesheet = await GetTimesheetByIdAsync(timesheetId);
        if (timesheet == null)
            throw new KeyNotFoundException("Timesheet não encontrado");

        if (timesheet.Status != "Draft" && timesheet.Status != "Rejected")
            throw new InvalidOperationException("Só é possível remover horas em timesheets em Draft ou Rejeitadas");

        var entry = timesheet.Entries.FirstOrDefault(e => e.ProjectId == projectId);
        if (entry != null)
        {
            timesheet.Entries.Remove(entry);
            timesheet.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return timesheet;
    }

    public async Task<Timesheet> SubmitTimesheetAsync(int id)
    {
        var timesheet = await GetTimesheetByIdAsync(id);
        if (timesheet == null)
            throw new KeyNotFoundException("Timesheet não encontrado");

        if (timesheet.Status != "Draft" && timesheet.Status != "Rejected")
            throw new InvalidOperationException("Apenas timesheets em Draft ou Rejeitadas podem ser submetidas");

        if (!timesheet.Entries.Any())
            throw new InvalidOperationException("Timesheet deve ter pelo menos uma entrada");

        timesheet.Status = "Submitted";
        timesheet.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return timesheet;
    }

    public async Task<Timesheet> ApproveTimesheetAsync(int id, int approverId)
    {
        var timesheet = await GetTimesheetByIdAsync(id);
        if (timesheet == null)
            throw new KeyNotFoundException("Timesheet não encontrado");

        if (timesheet.Status != "Submitted")
            throw new InvalidOperationException("Apenas timesheets submetidas podem ser aprovadas");

        timesheet.Status = "Approved";
        timesheet.ApprovedById = approverId;
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
            throw new InvalidOperationException("Apenas timesheets submetidas podem ser rejeitadas");

        timesheet.Status = "Rejected";
        timesheet.RejectionReason = reason;
        timesheet.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return timesheet;
    }

    public async Task DeleteTimesheetAsync(int id)
    {
        var timesheet = await GetTimesheetByIdAsync(id);
        if (timesheet == null)
            throw new KeyNotFoundException("Timesheet não encontrado");

        if (timesheet.Status != "Draft" && timesheet.Status != "Rejected")
            throw new InvalidOperationException("Apenas timesheets em Draft ou Rejeitadas podem ser deletadas");

        _context.Timesheets.Remove(timesheet);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Timesheet>> GetPendingApprovalsAsync(int setorId)
    {
        var query = _context.Timesheets
            .Where(t => t.Status == "Submitted")
            .Include(t => t.User)
            .ThenInclude(u => u.UserSetores)
            .Include(t => t.Entries)
            .ThenInclude(e => e.Project);

        // Se setorId > 0, filtra por setor específico (para Gestor)
        // Se setorId = 0, retorna todos (para Admin)
        if (setorId > 0)
        {
            return await query
                .Where(t => t.User!.UserSetores.Any(us => us.SetorId == setorId))
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        return await query
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public TimesheetResponse GetTimesheetResponse(Timesheet timesheet)
    {
        return new TimesheetResponse
        {
            Id = timesheet.Id,
            UserId = timesheet.UserId,
            UserName = timesheet.User?.FullName ?? "",
            Date = timesheet.Date,
            Status = timesheet.Status,
            TotalHours = timesheet.Entries.Sum(e => e.WorkHours),
            ApprovedAt = timesheet.ApprovedAt,
            RejectionReason = timesheet.RejectionReason,
            Entries = timesheet.Entries.Select(e => new TimesheetEntryResponse
            {
                Id = e.Id,
                ProjectId = e.ProjectId,
                ProjectName = e.Project?.Name ?? "",
                WorkHours = e.WorkHours,
                Notes = e.Notes
            }).ToList()
        };
    }

    public List<TimesheetListResponse> GetTimesheetListResponse(List<Timesheet> timesheets)
    {
        return timesheets.Select(t => new TimesheetListResponse
        {
            Id = t.Id,
            Date = t.Date,
            Status = t.Status,
            TotalHours = t.Entries.Sum(e => e.WorkHours),
            ProjectCount = t.Entries.Count,
            ApprovedAt = t.ApprovedAt
        }).ToList();
    }
}

public class TimesheetResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; }
    public decimal TotalHours { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public List<TimesheetEntryResponse> Entries { get; set; } = new();
}

public class TimesheetEntryResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }
    public decimal WorkHours { get; set; }
    public string? Notes { get; set; }
}

public class TimesheetListResponse
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; }
    public decimal TotalHours { get; set; }
    public int ProjectCount { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class RejectTimesheetRequest
{
    public string Reason { get; set; }
}
