namespace ProjectManagerWebAPI.Models;

public class Event
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty; // Formato: HH:mm
    public string EndTime { get; set; } = string.Empty;   // Formato: HH:mm
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public bool IsApplicableToProject { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Recorrência
    public int? ParentEventId { get; set; } // Referência ao evento original se for uma instância gerada
    public string? RecurrenceType { get; set; } // None, Daily, Weekly, Monthly, Yearly
    public string? RecurrenceDaysOfWeek { get; set; } // "Mon,Wed,Fri" para semanal
    public DateTime? RecurrenceEndDate { get; set; }
    public int? RecurrenceEndCount { get; set; } // Número de ocorrências, null se sem limite
    public bool IsRecurrenceParent { get; set; } // True se é o evento pai da recorrência
}

public enum RecurrenceType
{
    None,
    Daily,
    Weekly,
    Monthly,
    Yearly
}
