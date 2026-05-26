using Microsoft.EntityFrameworkCore;
using KMC.API.Models;

namespace KMC.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Unique email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // One organizer → many events
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organizer)
            .WithMany(u => u.OrganizedEvents)
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        // One user → many registrations
        modelBuilder.Entity<Registration>()
            .HasOne(r => r.User)
            .WithMany(u => u.Registrations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One event → many registrations
        modelBuilder.Entity<Registration>()
            .HasOne(r => r.Event)
            .WithMany(e => e.Registrations)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // A user can only register once per event
        modelBuilder.Entity<Registration>()
            .HasIndex(r => new { r.UserId, r.EventId })
            .IsUnique();
    }

    // ── Seed sample data ──────────────────────────────────────────────────────
    public void SeedData()
    {
        // Only seed if the database is empty
        if (Users.Any()) return;

        // ── Create organizer accounts ─────────────────────────────────────────
        var kmcOrganizer = new User
        {
            Name = "KMC Events Team",
            Email = "kmc@kandy.lk",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "Organizer",
            Source = "KMC",
            CreatedAt = DateTime.UtcNow
        };

        var extOrganizer = new User
        {
            Name = "Kandy Events Co.",
            Email = "organizer@kandyevents.lk",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "Organizer",
            Source = "Organizer",
            CreatedAt = DateTime.UtcNow
        };

        var publicUser = new User
        {
            Name = "Amara Silva",
            Email = "amara@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "Public",
            Source = "KMC",
            CreatedAt = DateTime.UtcNow
        };

        Users.AddRange(kmcOrganizer, extOrganizer, publicUser);
        SaveChanges();

        // ── Create sample events ──────────────────────────────────────────────
        var events = new List<Event>
        {
            // KMC Events
            new Event
            {
                Title       = "Kandy Esala Perahera",
                Description = "The grand Kandy Esala Perahera is one of the oldest and most magnificent Buddhist pageants in the world. Watch the sacred Tooth Relic of the Buddha being paraded through the streets of Kandy with traditional dancers, drummers, and decorated elephants.",
                Type        = "Cultural",
                Location    = "Dalada Maligawa, Kandy",
                Date        = DateTime.UtcNow.AddDays(15),
                Capacity    = 5000,
                Source      = "KMC",
                IsActive    = true,
                OrganizerId = kmcOrganizer.Id,
                CreatedAt   = DateTime.UtcNow
            },
            new Event
            {
                Title       = "Kandy City Marathon 2025",
                Description = "Join hundreds of runners for the annual Kandy City Marathon through the scenic streets of Kandy. Categories available for 5km, 10km, and full marathon. Open to all fitness levels. Registration includes a finisher medal and refreshments.",
                Type        = "Sports",
                Location    = "Kandy City Centre",
                Date        = DateTime.UtcNow.AddDays(22),
                Capacity    = 500,
                Source      = "KMC",
                IsActive    = true,
                OrganizerId = kmcOrganizer.Id,
                CreatedAt   = DateTime.UtcNow
            },
            new Event
            {
                Title       = "Kandy Lake Clean-Up Day",
                Description = "Be part of the community effort to keep Kandy Lake clean and beautiful. Join volunteers from across the city in this environmental initiative. Gloves, bags, and refreshments provided. A great opportunity to give back to the city.",
                Type        = "Community",
                Location    = "Kandy Lake, Kandy",
                Date        = DateTime.UtcNow.AddDays(8),
                Capacity    = 200,
                Source      = "KMC",
                IsActive    = true,
                OrganizerId = kmcOrganizer.Id,
                CreatedAt   = DateTime.UtcNow
            },
            new Event
            {
                Title       = "Kandy Food Festival",
                Description = "A celebration of Sri Lankan culinary culture featuring street food stalls, cooking demonstrations by celebrity chefs, and tastings from restaurants across the Kandy district. Enjoy traditional dishes alongside modern fusion cuisine.",
                Type        = "Food",
                Location    = "Kandy Municipal Grounds",
                Date        = DateTime.UtcNow.AddDays(30),
                Capacity    = 1000,
                Source      = "KMC",
                IsActive    = true,
                OrganizerId = kmcOrganizer.Id,
                CreatedAt   = DateTime.UtcNow
            },
            new Event
            {
                Title       = "Children's Day Celebration",
                Description = "A fun-filled day of activities for children including games, face painting, puppet shows, and cultural performances. Free entry for children under 12. A wonderful family outing in the heart of Kandy.",
                Type        = "Community",
                Location    = "Viharamahadevi Park, Kandy",
                Date        = DateTime.UtcNow.AddDays(45),
                Capacity    = 800,
                Source      = "KMC",
                IsActive    = true,
                OrganizerId = kmcOrganizer.Id,
                CreatedAt   = DateTime.UtcNow
            },

            // Organizer Company Events
            new Event
            {
                Title       = "Sunset Jazz Night",
                Description = "An elegant evening of live jazz music overlooking the Kandy hills. Featuring top local and international jazz artists. Tickets include a welcome cocktail and a selection of canapes. Dress code: smart casual.",
                Type        = "Music",
                Location    = "The Hill Club, Kandy",
                Date        = DateTime.UtcNow.AddDays(12),
                Capacity    = 150,
                Source      = "Organizer",
                IsActive    = true,
                OrganizerId = extOrganizer.Id,
                CreatedAt   = DateTime.UtcNow
            },
            new Event
            {
                Title       = "Tech Startup Summit Kandy",
                Description = "A full-day conference bringing together entrepreneurs, investors, and tech innovators from across Sri Lanka. Keynote speakers, panel discussions, networking sessions, and a startup pitch competition with prizes.",
                Type        = "Business",
                Location    = "Cinnamon Citadel Hotel, Kandy",
                Date        = DateTime.UtcNow.AddDays(20),
                Capacity    = 300,
                Source      = "Organizer",
                IsActive    = true,
                OrganizerId = extOrganizer.Id,
                CreatedAt   = DateTime.UtcNow
            },
            new Event
            {
                Title       = "Traditional Kandyan Dance Workshop",
                Description = "Learn the ancient art of Kandyan dance from master performers. This two-hour workshop covers the basic steps, costumes, and history of this UNESCO-recognised dance form. Suitable for beginners and all ages.",
                Type        = "Cultural",
                Location    = "Kandyan Arts Association Hall",
                Date        = DateTime.UtcNow.AddDays(5),
                Capacity    = 40,
                Source      = "Organizer",
                IsActive    = true,
                OrganizerId = extOrganizer.Id,
                CreatedAt   = DateTime.UtcNow
            },
            new Event
            {
                Title       = "Sri Lankan Cooking Masterclass",
                Description = "Learn to cook authentic Sri Lankan dishes with a professional chef. The class covers rice and curry, kottu roti, hoppers, and traditional desserts. All ingredients provided. Take home your own recipe booklet.",
                Type        = "Food",
                Location    = "Kandy Culinary Academy",
                Date        = DateTime.UtcNow.AddDays(18),
                Capacity    = 25,
                Source      = "Organizer",
                IsActive    = true,
                OrganizerId = extOrganizer.Id,
                CreatedAt   = DateTime.UtcNow
            },
            new Event
            {
                Title       = "Kandy Hills Trail Run",
                Description = "An exhilarating trail running event through the scenic hills surrounding Kandy. Routes of 8km and 15km available. The course winds through tea plantations and jungle trails with stunning views of the city.",
                Type        = "Sports",
                Location    = "Hantana Mountain Range, Kandy",
                Date        = DateTime.UtcNow.AddDays(35),
                Capacity    = 120,
                Source      = "Organizer",
                IsActive    = true,
                OrganizerId = extOrganizer.Id,
                CreatedAt   = DateTime.UtcNow
            }
        };

        Events.AddRange(events);
        SaveChanges();
    }

}