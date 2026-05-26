namespace KMC.API.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // "Public", "Organizer", "Admin"
    public string Role { get; set; } = "Public";

    // Which system registered this user: "KMC" or "Organizer"
    public string Source { get; set; } = "KMC";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}