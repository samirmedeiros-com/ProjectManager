using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagerWebAPI.Services;

public class ProjectUserCostService
{
    private readonly ApplicationDbContext _context;

    public ProjectUserCostService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProjectUserCostDto>> GetCostsByProjectAsync(int projectId)
    {
        var costs = await _context.ProjectUserCosts
            .Where(puc => puc.ProjectId == projectId)
            .Include(puc => puc.User)
            .AsNoTracking()
            .ToListAsync();

        return costs.Select(c => new ProjectUserCostDto
        {
            Id = c.Id,
            ProjectId = c.ProjectId,
            UserId = c.UserId,
            UserName = c.User?.FullName ?? "",
            CostPerHour = c.CostPerHour
        }).ToList();
    }

    public async Task<ProjectUserCostDto?> GetCostAsync(int projectId, int userId)
    {
        var cost = await _context.ProjectUserCosts
            .Where(puc => puc.ProjectId == projectId && puc.UserId == userId)
            .Include(puc => puc.User)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (cost == null)
            return null;

        return new ProjectUserCostDto
        {
            Id = cost.Id,
            ProjectId = cost.ProjectId,
            UserId = cost.UserId,
            UserName = cost.User?.FullName ?? "",
            CostPerHour = cost.CostPerHour
        };
    }

    public async Task<ProjectUserCostDto> CreateOrUpdateCostAsync(int projectId, int userId, decimal costPerHour)
    {
        var existingCost = await _context.ProjectUserCosts
            .FirstOrDefaultAsync(puc => puc.ProjectId == projectId && puc.UserId == userId);

        if (existingCost != null)
        {
            existingCost.CostPerHour = costPerHour;
            existingCost.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var newCost = new ProjectUserCost
            {
                ProjectId = projectId,
                UserId = userId,
                CostPerHour = costPerHour
            };
            _context.ProjectUserCosts.Add(newCost);
        }

        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        return new ProjectUserCostDto
        {
            Id = existingCost?.Id ?? (await _context.ProjectUserCosts
                .Where(puc => puc.ProjectId == projectId && puc.UserId == userId)
                .AsNoTracking()
                .FirstAsync()).Id,
            ProjectId = projectId,
            UserId = userId,
            UserName = user?.FullName ?? "",
            CostPerHour = costPerHour
        };
    }

    public async Task<bool> DeleteCostAsync(int projectId, int userId)
    {
        var cost = await _context.ProjectUserCosts
            .FirstOrDefaultAsync(puc => puc.ProjectId == projectId && puc.UserId == userId);

        if (cost == null)
            return false;

        _context.ProjectUserCosts.Remove(cost);
        await _context.SaveChangesAsync();
        return true;
    }
}

public class ProjectUserCostDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal CostPerHour { get; set; }
}
