namespace KMC.API.DTOs;

// ── Auth ──────────────────────────────────────────────────────────────────────

public record UserRegisterRequest(
    string Name,
    string Email,
    string Password,
    string Role,      // "Public" or "Organizer"
    string Source     // "KMC" or "Organizer"
);

public record UserLoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    string Name,
    string Email,
    string Role,
    string Source
);

// ── Events ────────────────────────────────────────────────────────────────────

public record CreateEventRequest(
    string Title,
    string Description,
    string Type,
    string Location,
    DateTime Date,
    int Capacity,
    string Source    // "KMC" or "Organizer"
);

public record UpdateEventRequest(
    string Title,
    string Description,
    string Type,
    string Location,
    DateTime Date,
    int Capacity
);

public record EventResponse(
    int Id,
    string Title,
    string Description,
    string Type,
    string Location,
    DateTime Date,
    int Capacity,
    int RegistrationCount,
    string Source,
    string OrganizerName,
    bool IsActive,
    DateTime CreatedAt
);

// ── Registrations ─────────────────────────────────────────────────────────────

public record RegisterForEventRequest(
    int EventId,
    string RegisteredFrom  // "KMC" or "Organizer"
);

public record RegistrationResponse(
    int Id,
    int EventId,
    string EventTitle,
    string EventDate,
    string UserName,
    string UserEmail,
    string RegisteredFrom,
    DateTime RegisteredAt
);