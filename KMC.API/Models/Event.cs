namespace KMC.API.Models;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // e.g. "Cultural", "Sports", "Music", "Community", "Food"
    public string Type { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Capacity { get; set; }

    // "KMC" = created via KMC site, "Organizer" = created via organizer site
    public string Source { get; set; } = "KMC";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // FK to the organizer (User with Role = "Organizer")
    public int OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}