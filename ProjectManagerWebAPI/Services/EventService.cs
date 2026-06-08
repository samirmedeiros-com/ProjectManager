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
            // Recurrence fields are temporarily disabled until database columns are created
            // RecurrenceType = request.RecurrenceType,
            // RecurrenceDaysOfWeek = request.RecurrenceDaysOfWeek,
            // RecurrenceEndDate = request.RecurrenceEndDate,
            // RecurrenceEndCount = request.RecurrenceEndCount,
            // IsRecurrenceParent = !string.IsNullOrEmpty(request.RecurrenceType) && request.RecurrenceType != "None",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(@event);
        await _context.SaveChangesAsync();

        return @event;
    }

    private async Task GenerateRecurringEventsAsync(Event parentEvent, CreateEventRequest request)
    {
        var recurringEvents = new List<Event>();
        DateTime currentDate = parentEvent.Date;
        int occurrenceCount = 0;

        while (true)
        {
            // Verificar limite de data
            if (request.RecurrenceEndDate.HasValue && currentDate > request.RecurrenceEndDate.Value)
                break;

            // Verificar limite de ocorrências
            if (request.RecurrenceEndCount.HasValue && occurrenceCount >= request.RecurrenceEndCount.Value)
                break;

            // Gerar evento recorrente
            if (ShouldCreateEventOnDate(currentDate, request.RecurrenceType, request.RecurrenceDaysOfWeek))
            {
                var recurringEvent = new Event
                {
                    UserId = parentEvent.UserId,
                    Title = parentEvent.Title,
                    Description = parentEvent.Description,
                    Date = currentDate,
                    StartTime = parentEvent.StartTime,
                    EndTime = parentEvent.EndTime,
                    ProjectId = parentEvent.ProjectId,
                    IsApplicableToProject = parentEvent.IsApplicableToProject,
                    ParentEventId = parentEvent.Id,
                    IsRecurrenceParent = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                recurringEvents.Add(recurringEvent);
                occurrenceCount++;
            }

            // Avançar para o próximo dia/semana/mês/ano
            currentDate = GetNextDate(currentDate, request.RecurrenceType);
        }

        _context.Events.AddRange(recurringEvents);
        await _context.SaveChangesAsync();
    }

    private bool ShouldCreateEventOnDate(DateTime date, string? recurrenceType, string? daysOfWeek)
    {
        if (string.IsNullOrEmpty(recurrenceType) || recurrenceType == "None")
            return false;

        if (recurrenceType == "Daily")
            return true;

        if (recurrenceType == "Weekly" && !string.IsNullOrEmpty(daysOfWeek))
        {
            var dayName = date.ToString("ddd");
            return daysOfWeek.Contains(dayName.Substring(0, 1).ToUpper() + dayName.Substring(1).ToLower());
        }

        if (recurrenceType == "Monthly")
            return true; // Mesmo dia do mês

        if (recurrenceType == "Yearly")
            return true; // Mesmo dia do ano

        return false;
    }

    private DateTime GetNextDate(DateTime current, string? recurrenceType)
    {
        return recurrenceType switch
        {
            "Daily" => current.AddDays(1),
            "Weekly" => current.AddDays(1), // Vai incrementar dia a dia, ShouldCreateEventOnDate faz filtro
            "Monthly" => current.AddMonths(1),
            "Yearly" => current.AddYears(1),
            _ => current.AddDays(1)
        };
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

    public async Task DeleteRecurrenceSeriesAsync(int id)
    {
        var @event = await GetEventByIdAsync(id);
        if (@event == null)
            throw new KeyNotFoundException("Evento não encontrado");

        int parentId = @event.IsRecurrenceParent ? @event.Id : (@event.ParentEventId ?? @event.Id);

        // Deletar evento pai e todas as instâncias
        var eventsToDelete = await _context.Events
            .Where(e => e.Id == parentId || e.ParentEventId == parentId)
            .ToListAsync();

        _context.Events.RemoveRange(eventsToDelete);
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
            IsApplicableToProject = e.IsApplicableToProject,
            // Recurrence fields disabled until DB columns exist
            RecurrenceType = null,
            IsRecurrenceParent = false,
            ParentEventId = null
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

    // Recorrência
    public string? RecurrenceType { get; set; } // None, Daily, Weekly, Monthly, Yearly
    public string? RecurrenceDaysOfWeek { get; set; } // "Mon,Wed,Fri"
    public DateTime? RecurrenceEndDate { get; set; }
    public int? RecurrenceEndCount { get; set; }
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

    // Recorrência
    public string? RecurrenceType { get; set; }
    public string? RecurrenceDaysOfWeek { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public int? RecurrenceEndCount { get; set; }
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

    // Recorrência
    public string? RecurrenceType { get; set; }
    public bool IsRecurrenceParent { get; set; }
    public int? ParentEventId { get; set; }
}
