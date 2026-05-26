using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KMC.API.Data;
using KMC.API.DTOs;
using KMC.API.Models;

namespace KMC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all events — supports filtering by type, date, source, and keyword search.
    /// This is the shared feed shown on BOTH the KMC and Organizer websites.
    /// </summary>
    /// <param name="type">Filter by event type e.g. Cultural, Sports, Music</param>
    /// <param name="source">Filter by source: KMC or Organizer</param>
    /// <param name="date">Filter by exact date (yyyy-MM-dd)</param>
    /// <param name="search">Search in title and description</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? type,
        [FromQuery] string? source,
        [FromQuery] DateTime? date,
        [FromQuery] string? search)
    {
        var query = _db.Events
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .Where(e => e.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.Type.ToLower() == type.ToLower());

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(e => e.Source.ToLower() == source.ToLower());

        if (date.HasValue)
            query = query.Where(e => e.Date.Date == date.Value.Date);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e =>
                e.Title.Contains(search) ||
                e.Description.Contains(search));

        var events = await query
            .OrderBy(e => e.Date)
            .Select(e => MapToResponse(e))
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Get a single event by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

        if (ev == null) return NotFound(new { message = "Event not found." });

        return Ok(MapToResponse(ev));
    }

    /// <summary>
    /// Create a new event. Only users with the Organizer role can do this.
    /// Works from both the KMC site and the Organizer site — set Source accordingly.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(EventResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var organizerId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ev = new Event
        {
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            Location = request.Location,
            Date = request.Date,
            Capacity = request.Capacity,
            Source = request.Source,
            OrganizerId = organizerId
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(ev).Reference(e => e.Organizer).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, MapToResponse(ev));
    }

    /// <summary>
    /// Update an event. Only the organizer who CREATED the event can update it.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(EventResponse), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var ev = await _db.Events
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

        if (ev == null) return NotFound(new { message = "Event not found." });

        // Only the creator (or an Admin) can update
        if (ev.OrganizerId != userId && role != "Admin")
            return Forbid();

        ev.Title = request.Title;
        ev.Description = request.Description;
        ev.Type = request.Type;
        ev.Location = request.Location;
        ev.Date = request.Date;
        ev.Capacity = request.Capacity;

        await _db.SaveChangesAsync();
        return Ok(MapToResponse(ev));
    }

    /// <summary>
    /// Delete (soft-delete) an event. Only the creator or Admin can do this.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var ev = await _db.Events.FindAsync(id);
        if (ev == null || !ev.IsActive)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganizerId != userId && role != "Admin")
            return Forbid();

        ev.IsActive = false; // soft delete
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static EventResponse MapToResponse(Event e) => new(
        e.Id,
        e.Title,
        e.Description,
        e.Type,
        e.Location,
        e.Date,
        e.Capacity,
        e.Registrations.Count,
        e.Source,
        e.Organizer?.Name ?? "Unknown",
        e.IsActive,
        e.CreatedAt
    );
}