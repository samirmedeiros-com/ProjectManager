using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Services;
using System.Security.Claims;

namespace ProjectManagerWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly EventService _eventService;

    public EventsController(EventService eventService)
    {
        _eventService = eventService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim?.Value ?? "0");
    }

    [HttpGet]
    public async Task<IActionResult> GetUserEvents([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var userId = GetUserId();
        var events = await _eventService.GetUserEventsAsync(userId, startDate, endDate);
        var responses = _eventService.GetEventResponses(events);
        return Ok(responses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var @event = await _eventService.GetEventByIdAsync(id);
        if (@event == null)
            return NotFound();

        if (@event.UserId != GetUserId())
            return Forbid();

        return Ok(@event);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        var userId = GetUserId();
        var @event = await _eventService.CreateEventAsync(userId, request);
        return CreatedAtAction(nameof(GetEvent), new { id = @event.Id }, @event);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
    {
        var userId = GetUserId();
        var @event = await _eventService.GetEventByIdAsync(id);
        if (@event == null)
            return NotFound();

        if (@event.UserId != userId)
            return Forbid();

        var updated = await _eventService.UpdateEventAsync(id, request);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id, [FromQuery] bool deleteAll = false)
    {
        var userId = GetUserId();
        var @event = await _eventService.GetEventByIdAsync(id);
        if (@event == null)
            return NotFound();

        if (@event.UserId != userId)
            return Forbid();

        if (deleteAll && (@event.IsRecurrenceParent || @event.ParentEventId.HasValue))
        {
            await _eventService.DeleteRecurrenceSeriesAsync(id);
        }
        else
        {
            await _eventService.DeleteEventAsync(id);
        }

        return NoContent();
    }

    [HttpOptions("{*path}")]
    [AllowAnonymous]
    public IActionResult PreflightHandler(string path)
    {
        return Ok();
    }
}
