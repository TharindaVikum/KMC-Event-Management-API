namespace KMC.API.Models;

public class Registration
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    // "KMC" or "Organizer" — which site the registration came from
    public string RegisteredFrom { get; set; } = "KMC";

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}