using ProjectManagerWebAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagerWebAPI.Services;

public class ReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<HoursByMonthDto>> GetHoursByMonthAsync(int? userId = null)
    {
        var timesheets = await _context.Timesheets
            .Where(t => t.Status == "Approved")
            .Include(t => t.Entries)
            .AsNoTracking()
            .ToListAsync();

        if (userId.HasValue)
            timesheets = timesheets.Where(t => t.UserId == userId.Value).ToList();

        var result = timesheets
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new HoursByMonthDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalHours = g.SelectMany(t => t.Entries).Sum(e => e.WorkHours)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        return result;
    }

    public async Task<List<HoursByProjectDto>> GetHoursByProjectAsync(int? userId = null, int? monthOffset = null)
    {
        var timesheets = await _context.Timesheets
            .Where(t => t.Status == "Approved")
            .Include(t => t.Entries)
            .ThenInclude(e => e.Project)
            .AsNoTracking()
            .ToListAsync();

        if (userId.HasValue)
            timesheets = timesheets.Where(t => t.UserId == userId.Value).ToList();

        if (monthOffset.HasValue && monthOffset.Value > 0)
        {
            var targetDate = DateTime.UtcNow.AddMonths(-monthOffset.Value);
            var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            timesheets = timesheets.Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth).ToList();
        }

        var result = timesheets
            .SelectMany(t => t.Entries)
            .GroupBy(e => new { e.ProjectId, e.Project!.Name })
            .Select(g => new HoursByProjectDto
            {
                ProjectId = g.Key.ProjectId,
                ProjectName = g.Key.Name,
                TotalHours = g.Sum(e => e.WorkHours)
            })
            .OrderByDescending(x => x.TotalHours)
            .ToList();

        return result;
    }

    public async Task<List<HoursByUserDto>> GetHoursByUserAsync(int? monthOffset = null)
    {
        var timesheets = await _context.Timesheets
            .Where(t => t.Status == "Approved")
            .Include(t => t.User)
            .Include(t => t.Entries)
            .AsNoTracking()
            .ToListAsync();

        if (monthOffset.HasValue && monthOffset.Value > 0)
        {
            var targetDate = DateTime.UtcNow.AddMonths(-monthOffset.Value);
            var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            timesheets = timesheets.Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth).ToList();
        }

        var result = timesheets
            .GroupBy(t => new { t.UserId, t.User!.FullName })
            .Select(g => new HoursByUserDto
            {
                UserId = g.Key.UserId,
                UserName = g.Key.FullName,
                TotalHours = g.SelectMany(t => t.Entries).Sum(e => e.WorkHours)
            })
            .OrderByDescending(x => x.TotalHours)
            .ToList();

        return result;
    }

    public async Task<ReportSummaryDto> GetReportSummaryAsync(int? monthOffset = null)
    {
        var timesheets = await _context.Timesheets
            .Where(t => t.Status == "Approved")
            .Include(t => t.Entries)
            .ThenInclude(e => e.Project)
            .Include(t => t.User)
            .AsNoTracking()
            .ToListAsync();

        if (monthOffset.HasValue && monthOffset.Value > 0)
        {
            var targetDate = DateTime.UtcNow.AddMonths(-monthOffset.Value);
            var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            timesheets = timesheets.Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth).ToList();
        }

        var entries = timesheets.SelectMany(t => t.Entries).ToList();

        return new ReportSummaryDto
        {
            TotalHours = entries.Sum(e => e.WorkHours),
            TotalTimesheets = timesheets.Count,
            UniqueUsers = timesheets.Select(t => t.UserId).Distinct().Count(),
            UniqueProjects = entries.Select(e => e.ProjectId).Distinct().Count(),
            HoursByMonth = await GetHoursByMonthAsync(),
            HoursByProject = await GetHoursByProjectAsync(null, monthOffset),
            HoursByUser = await GetHoursByUserAsync(monthOffset)
        };
    }
}

public class HoursByMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalHours { get; set; }
}

public class HoursByProjectDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
}

public class HoursByUserDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
}

public class ReportSummaryDto
{
    public decimal TotalHours { get; set; }
    public int TotalTimesheets { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueProjects { get; set; }
    public List<HoursByMonthDto> HoursByMonth { get; set; } = new();
    public List<HoursByProjectDto> HoursByProject { get; set; } = new();
    public List<HoursByUserDto> HoursByUser { get; set; } = new();
}
