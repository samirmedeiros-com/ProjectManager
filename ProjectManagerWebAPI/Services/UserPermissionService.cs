using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagerWebAPI.Services;

public interface IUserPermissionService
{
    Task<bool> IsGestorOfSetor(int gestorId, int setorId);
    Task<bool> CanManageUser(int currentUserId, int targetUserId);
    Task<List<int>> GetAllowedSetoresForUser(int userId);
    Task<List<User>> GetManagedUsers(int managerUserId);
}

public class UserPermissionService : IUserPermissionService
{
    private readonly ApplicationDbContext _context;

    public UserPermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsGestorOfSetor(int gestorId, int setorId)
    {
        return await _context.UserSetores
            .AnyAsync(us => us.UserId == gestorId && us.SetorId == setorId);
    }

    public async Task<bool> CanManageUser(int currentUserId, int targetUserId)
    {
        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser?.Role == "Admin") return true;

        if (currentUser?.Role != "Gestor") return false;

        var currentUserSetores = await _context.UserSetores
            .Where(us => us.UserId == currentUserId)
            .Select(us => us.SetorId)
            .ToListAsync();

        var targetUserSetores = await _context.UserSetores
            .Where(us => us.UserId == targetUserId)
            .Select(us => us.SetorId)
            .ToListAsync();

        return targetUserSetores.Any(s => currentUserSetores.Contains(s));
    }

    public async Task<List<int>> GetAllowedSetoresForUser(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.Role == "Admin")
        {
            return await _context.Setores.Select(s => s.Id).ToListAsync();
        }

        return await _context.UserSetores
            .Where(us => us.UserId == userId)
            .Select(us => us.SetorId)
            .ToListAsync();
    }

    public async Task<List<User>> GetManagedUsers(int managerUserId)
    {
        var manager = await _context.Users.FindAsync(managerUserId);
        if (manager?.Role == "Admin")
        {
            return await _context.Users.OrderBy(u => u.FullName).ToListAsync();
        }

        if (manager?.Role != "Gestor")
            return new List<User>();

        var managerSetores = await _context.UserSetores
            .Where(us => us.UserId == managerUserId)
            .Select(us => us.SetorId)
            .ToListAsync();

        return await _context.Users
            .Where(u => u.UserSetores.Any(us => managerSetores.Contains(us.SetorId)))
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }
}
