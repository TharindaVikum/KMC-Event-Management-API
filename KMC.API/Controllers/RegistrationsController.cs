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
[Tags("Registrations")]
public class RegistrationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RegistrationsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Register the logged-in user for an event.
    /// Works from both KMC and Organizer sites — set RegisteredFrom accordingly.
    /// The registration is SHARED: it will appear on both sites immediately.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(RegistrationResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register([FromBody] RegisterForEventRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ev = await _db.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId && e.IsActive);

        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.Registrations.Count >= ev.Capacity)
            return BadRequest(new { message = "This event is fully booked." });

        bool alreadyRegistered = await _db.Registrations
            .AnyAsync(r => r.UserId == userId && r.EventId == request.EventId);

        if (alreadyRegistered)
            return Conflict(new { message = "You are already registered for this event." });

        var reg = new Registration
        {
            UserId = userId,
            EventId = request.EventId,
            RegisteredFrom = request.RegisteredFrom
        };

        _db.Registrations.Add(reg);
        await _db.SaveChangesAsync();

        await _db.Entry(reg).Reference(r => r.User).LoadAsync();
        await _db.Entry(reg).Reference(r => r.Event).LoadAsync();

        return CreatedAtAction(nameof(GetMyRegistrations), MapToResponse(reg));
    }

    /// <summary>
    /// Get all registrations for the currently logged-in user.
    /// Shows registrations made from BOTH sites in one list.
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(List<RegistrationResponse>), 200)]
    public async Task<IActionResult> GetMyRegistrations()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var registrations = await _db.Registrations
            .Include(r => r.Event)
            .Include(r => r.User)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RegisteredAt)
            .Select(r => MapToResponse(r))
            .ToListAsync();

        return Ok(registrations);
    }

    /// <summary>
    /// Get all registrations for a specific event.
    /// Only the event's organizer or an Admin can see this.
    /// Shows who registered from KMC and who from the Organizer site.
    /// </summary>
    [HttpGet("event/{eventId}")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(List<RegistrationResponse>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetEventRegistrations(int eventId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        if (ev.OrganizerId != userId && role != "Admin")
            return Forbid();

        var registrations = await _db.Registrations
            .Include(r => r.User)
            .Include(r => r.Event)
            .Where(r => r.EventId == eventId)
            .OrderByDescending(r => r.RegisteredAt)
            .Select(r => MapToResponse(r))
            .ToListAsync();

        return Ok(registrations);
    }

    /// <summary>
    /// Cancel (delete) a registration. Only the user themselves can cancel.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var reg = await _db.Registrations.FindAsync(id);
        if (reg == null) return NotFound(new { message = "Registration not found." });

        if (reg.UserId != userId)
            return Forbid();

        _db.Registrations.Remove(reg);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static RegistrationResponse MapToResponse(Registration r) => new(
        r.Id,
        r.EventId,
        r.Event?.Title ?? "",
        r.Event?.Date.ToString("yyyy-MM-dd") ?? "",
        r.User?.Name ?? "",
        r.User?.Email ?? "",
        r.RegisteredFrom,
        r.RegisteredAt
    );
}