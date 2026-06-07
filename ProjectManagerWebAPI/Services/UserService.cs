using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagerWebAPI.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<List<UserDto>> GetAllUsersAsync(int currentUserId);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request, int currentUserId, string currentUserRole);
    Task<bool> DeleteUserAsync(int id);
    Task<UserDto?> DeactivateUserAsync(int id);
    Task<UserDto?> ActivateUserAsync(int id);
    Task<UserDto?> AssignSetoresAsync(int userId, List<int> setorIds);
    Task<List<SetorDto>?> GetUserSetoresAsync(int userId);
    Task<bool> RemoveSetorAsync(int userId, int setorId);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserPermissionService? _permissionService;

    public UserService(ApplicationDbContext context, IUserPermissionService? permissionService = null)
    {
        _context = context;
        _permissionService = permissionService;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.UserSetores)
            .ThenInclude(us => us.Setor)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }

    public async Task<List<UserDto>> GetAllUsersAsync(int currentUserId)
    {
        var user = await _context.Users.FindAsync(currentUserId);

        // Admin vê todos os users
        if (user?.Role == "Admin")
        {
            var allUsers = await _context.Users
                .Include(u => u.UserSetores)
                .ThenInclude(us => us.Setor)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            return allUsers.Select(MapToDto).ToList();
        }

        // Gestor vê apenas users dos seus setores
        if (user?.Role == "Gestor")
        {
            var managerSetores = await _context.UserSetores
                .Where(us => us.UserId == currentUserId)
                .Select(us => us.SetorId)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => u.UserSetores.Any(us => managerSetores.Contains(us.SetorId)))
                .Include(u => u.UserSetores)
                .ThenInclude(us => us.Setor)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return users.Select(MapToDto).ToList();
        }

        // Outros roles não têm acesso
        return new List<UserDto>();
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserSetores)
            .ThenInclude(us => us.Setor)
            .FirstOrDefaultAsync(u => u.Id == id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        return await UpdateUserAsync(id, request, 0, "Admin");
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request, int currentUserId, string currentUserRole)
    {
        var user = await _context.Users
            .Include(u => u.UserSetores)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return null;

        if (currentUserRole == "Gestor" && _permissionService != null)
        {
            var canManage = await _permissionService.CanManageUser(currentUserId, id);
            if (!canManage)
                throw new UnauthorizedAccessException("Sem acesso para editar este utilizador");

            var allowedSetores = await _permissionService.GetAllowedSetoresForUser(currentUserId);
            var newSetorIds = request.SetorIds ?? new();

            foreach (var setorId in newSetorIds)
            {
                if (!allowedSetores.Contains(setorId))
                    throw new UnauthorizedAccessException($"Sem acesso ao setor {setorId}");
            }
        }

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.Department = request.Department;

        var existingSetorIds = user.UserSetores.Select(us => us.SetorId).ToList();
        var newSetorIds2 = request.SetorIds ?? new();

        var setoresToRemove = existingSetorIds.Except(newSetorIds2).ToList();
        foreach (var setorId in setoresToRemove)
        {
            var userSetor = user.UserSetores.FirstOrDefault(us => us.SetorId == setorId);
            if (userSetor != null)
                user.UserSetores.Remove(userSetor);
        }

        var setoresToAdd = newSetorIds2.Except(existingSetorIds).ToList();
        foreach (var setorId in setoresToAdd)
        {
            user.UserSetores.Add(new UserSetor { UserId = id, SetorId = setorId });
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(id);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<UserDto?> DeactivateUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        user.IsActive = false;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(id);
    }

    public async Task<UserDto?> ActivateUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        user.IsActive = true;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(id);
    }

    public async Task<UserDto?> AssignSetoresAsync(int userId, List<int> setorIds)
    {
        var user = await _context.Users
            .Include(u => u.UserSetores)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        user.UserSetores.Clear();

        foreach (var setorId in setorIds)
        {
            user.UserSetores.Add(new UserSetor { UserId = userId, SetorId = setorId });
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(userId);
    }

    public async Task<List<SetorDto>?> GetUserSetoresAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.UserSetores)
            .ThenInclude(us => us.Setor)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        return user.UserSetores
            .Select(us => new SetorDto
            {
                Id = us.Setor.Id,
                Name = us.Setor.Name,
                Description = us.Setor.Description,
                IsActive = us.Setor.IsActive,
                CreatedAt = us.Setor.CreatedAt,
                UpdatedAt = us.Setor.UpdatedAt
            })
            .ToList();
    }

    public async Task<bool> RemoveSetorAsync(int userId, int setorId)
    {
        var userSetor = await _context.UserSetores
            .FirstOrDefaultAsync(us => us.UserId == userId && us.SetorId == setorId);
        if (userSetor == null) return false;

        _context.UserSetores.Remove(userSetor);
        await _context.SaveChangesAsync();

        return true;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Department = user.Department,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Setores = user.UserSetores
                .Select(us => new SetorDto
                {
                    Id = us.Setor.Id,
                    Name = us.Setor.Name,
                    Description = us.Setor.Description,
                    IsActive = us.Setor.IsActive,
                    CreatedAt = us.Setor.CreatedAt,
                    UpdatedAt = us.Setor.UpdatedAt
                })
                .ToList()
        };
    }
}
