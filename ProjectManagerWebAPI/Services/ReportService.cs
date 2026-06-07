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

    public async Task<List<HoursByProjectWithCostDto>> GetHoursByProjectWithCostAsync(int? userId = null, int? monthOffset = null)
    {
        var timesheets = await _context.Timesheets
            .Where(t => t.Status == "Approved")
            .Include(t => t.Entries)
            .ThenInclude(e => e.Project)
            .Include(t => t.User)
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

        // Carregar todos os custos para referência rápida
        var allCosts = await _context.ProjectUserCosts.AsNoTracking().ToListAsync();

        var result = timesheets
            .SelectMany(t => t.Entries.Select(e => new { Timesheet = t, Entry = e }))
            .GroupBy(x => new { x.Entry.ProjectId, x.Entry.Project!.Name })
            .Select(g => new HoursByProjectWithCostDto
            {
                ProjectId = g.Key.ProjectId,
                ProjectName = g.Key.Name,
                TotalHours = g.Sum(x => x.Entry.WorkHours),
                TotalCost = g.Sum(x =>
                {
                    var cost = allCosts.FirstOrDefault(c =>
                        c.ProjectId == x.Entry.ProjectId && c.UserId == x.Timesheet.UserId);
                    return cost != null ? x.Entry.WorkHours * cost.CostPerHour : 0;
                })
            })
            .OrderByDescending(x => x.TotalHours)
            .ToList();

        return result;
    }

    public async Task<List<HoursByUserWithCostDto>> GetHoursByUserWithCostAsync(int? monthOffset = null)
    {
        var timesheets = await _context.Timesheets
            .Where(t => t.Status == "Approved")
            .Include(t => t.User)
            .Include(t => t.Entries)
            .ThenInclude(e => e.Project)
            .AsNoTracking()
            .ToListAsync();

        if (monthOffset.HasValue && monthOffset.Value > 0)
        {
            var targetDate = DateTime.UtcNow.AddMonths(-monthOffset.Value);
            var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            timesheets = timesheets.Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth).ToList();
        }

        // Carregar todos os custos para referência rápida
        var allCosts = await _context.ProjectUserCosts.AsNoTracking().ToListAsync();

        var result = timesheets
            .GroupBy(t => new { t.UserId, t.User!.FullName })
            .Select(g => new HoursByUserWithCostDto
            {
                UserId = g.Key.UserId,
                UserName = g.Key.FullName,
                TotalHours = g.SelectMany(t => t.Entries).Sum(e => e.WorkHours),
                TotalCost = g.SelectMany(t => t.Entries).Sum(e =>
                {
                    var cost = allCosts.FirstOrDefault(c =>
                        c.ProjectId == e.ProjectId && c.UserId == g.Key.UserId);
                    return cost != null ? e.WorkHours * cost.CostPerHour : 0;
                })
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

        // Carregar todos os custos para cálculo
        var allCosts = await _context.ProjectUserCosts.AsNoTracking().ToListAsync();

        var totalCost = timesheets.SelectMany(t => t.Entries).Sum(e =>
        {
            var timesheet = timesheets.First(t => t.Entries.Contains(e));
            var cost = allCosts.FirstOrDefault(c => c.ProjectId == e.ProjectId && c.UserId == timesheet.UserId);
            return cost != null ? e.WorkHours * cost.CostPerHour : 0;
        });

        return new ReportSummaryDto
        {
            TotalHours = entries.Sum(e => e.WorkHours),
            TotalCost = totalCost,
            TotalTimesheets = timesheets.Count,
            UniqueUsers = timesheets.Select(t => t.UserId).Distinct().Count(),
            UniqueProjects = entries.Select(e => e.ProjectId).Distinct().Count(),
            HoursByMonth = await GetHoursByMonthAsync(),
            HoursByProject = await GetHoursByProjectWithCostAsync(null, monthOffset),
            HoursByUser = await GetHoursByUserWithCostAsync(monthOffset)
        };
    }

    public async Task<List<HoursByMonthDto>> GetHoursByMonthAsync()
    {
        return await GetHoursByMonthAsync(null);
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

public class HoursByProjectWithCostDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public decimal TotalCost { get; set; }
}

public class HoursByUserDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
}

public class HoursByUserWithCostDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public decimal TotalCost { get; set; }
}

public class ReportSummaryDto
{
    public decimal TotalHours { get; set; }
    public decimal TotalCost { get; set; }
    public int TotalTimesheets { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueProjects { get; set; }
    public List<HoursByMonthDto> HoursByMonth { get; set; } = new();
    public List<HoursByProjectWithCostDto> HoursByProject { get; set; } = new();
    public List<HoursByUserWithCostDto> HoursByUser { get; set; } = new();
}
