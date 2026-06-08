using ProjectManagerWebAPI.Data;
using ProjectManagerWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagerWebAPI.Services;

public class EventService
{
    private readonly ApplicationDbContext _context;

    public EventService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Event>> GetUserEventsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Events
            .Where(e => e.UserId == userId)
            .Include(e => e.Project)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(e => e.Date >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(e => e.Date <= endDate.Value.Date);

        return await query.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToListAsync();
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        return await _context.Events
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Event> CreateEventAsync(int userId, CreateEventRequest request)
    {
        var @event = new Event
        {
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            ProjectId = request.ProjectId,
            IsApplicableToProject = request.IsApplicableToProject,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(@event);
        await _context.SaveChangesAsync();
        return @event;
    }

    public async Task<Event> UpdateEventAsync(int id, UpdateEventRequest request)
    {
        var @event = await GetEventByIdAsync(id);
        if (@event == null)
            throw new KeyNotFoundException("Evento não encontrado");

        @event.Title = request.Title;
        @event.Description = request.Description;
        @event.Date = request.Date;
        @event.StartTime = request.StartTime;
        @event.EndTime = request.EndTime;
        @event.ProjectId = request.ProjectId;
        @event.IsApplicableToProject = request.IsApplicableToProject;
        @event.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return @event;
    }

    public async Task DeleteEventAsync(int id)
    {
        var @event = await GetEventByIdAsync(id);
        if (@event == null)
            throw new KeyNotFoundException("Evento não encontrado");

        _context.Events.Remove(@event);
        await _context.SaveChangesAsync();
    }

    public List<EventResponse> GetEventResponses(List<Event> events)
    {
        return events.Select(e => new EventResponse
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            Date = e.Date,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            ProjectId = e.ProjectId,
            ProjectName = e.Project?.Name ?? "",
            IsApplicableToProject = e.IsApplicableToProject
        }).ToList();
    }
}

public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty; // Formato: HH:mm
    public string EndTime { get; set; } = string.Empty;   // Formato: HH:mm
    public int? ProjectId { get; set; }
    public bool IsApplicableToProject { get; set; }
}

public class UpdateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty; // Formato: HH:mm
    public string EndTime { get; set; } = string.Empty;   // Formato: HH:mm
    public int? ProjectId { get; set; }
    public bool IsApplicableToProject { get; set; }
}

public class EventResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public bool IsApplicableToProject { get; set; }
}
