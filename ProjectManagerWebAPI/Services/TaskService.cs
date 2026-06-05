using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Services;

public interface ITaskService
{
    Task<List<ProjectTaskDto>> GetTasksByProject(int projectId);
    Task<ProjectTaskDto?> GetTaskById(int id);
    Task<ProjectTaskDto> CreateTask(int projectId, CreateTaskRequest request);
    Task<ProjectTaskDto?> UpdateTask(int id, UpdateTaskRequest request);
    Task<bool> DeleteTask(int id);
}

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;

    public TaskService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProjectTaskDto>> GetTasksByProject(int projectId)
    {
        var tasks = await _context.ProjectTasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        return tasks.Select(MapToDto).ToList();
    }

    public async Task<ProjectTaskDto?> GetTaskById(int id)
    {
        var task = await _context.ProjectTasks.FindAsync(id);
        return task == null ? null : MapToDto(task);
    }

    public async Task<ProjectTaskDto> CreateTask(int projectId, CreateTaskRequest request)
    {
        var task = new ProjectTask
        {
            ProjectId = projectId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            AssignedTo = request.AssignedTo,
            EstimatedHours = request.EstimatedHours,
            Priority = request.Priority ?? "Medium",
            Status = "Pending"
        };

        _context.ProjectTasks.Add(task);
        await _context.SaveChangesAsync();

        return MapToDto(task);
    }

    public async Task<ProjectTaskDto?> UpdateTask(int id, UpdateTaskRequest request)
    {
        var task = await _context.ProjectTasks.FindAsync(id);
        if (task == null) return null;

        if (!string.IsNullOrEmpty(request.Title)) task.Title = request.Title;
        if (!string.IsNullOrEmpty(request.Description)) task.Description = request.Description;
        if (request.DueDate.HasValue) task.DueDate = request.DueDate.Value;
        if (!string.IsNullOrEmpty(request.Status)) task.Status = request.Status;
        if (!string.IsNullOrEmpty(request.Priority)) task.Priority = request.Priority;
        if (!string.IsNullOrEmpty(request.AssignedTo)) task.AssignedTo = request.AssignedTo;
        if (request.ActualHours.HasValue) task.ActualHours = request.ActualHours.Value;
        if (request.Progress.HasValue) task.Progress = request.Progress.Value;

        task.UpdatedAt = DateTime.UtcNow;

        _context.ProjectTasks.Update(task);
        await _context.SaveChangesAsync();

        return MapToDto(task);
    }

    public async Task<bool> DeleteTask(int id)
    {
        var task = await _context.ProjectTasks.FindAsync(id);
        if (task == null) return false;

        _context.ProjectTasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }

    private ProjectTaskDto MapToDto(ProjectTask task)
    {
        return new ProjectTaskDto
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            AssignedTo = task.AssignedTo,
            EstimatedHours = task.EstimatedHours,
            ActualHours = task.ActualHours,
            Progress = task.Progress,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
