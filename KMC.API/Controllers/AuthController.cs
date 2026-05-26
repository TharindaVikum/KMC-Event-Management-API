using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KMC.API.Data;
using KMC.API.DTOs;
using KMC.API.Models;
using KMC.API.Services;

namespace KMC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    /// <summary>Register a new user (works for both KMC and Organizer sites)</summary>
    /// <remarks>
    /// Set Source = "KMC" when registering from the KMC website.
    /// Set Source = "Organizer" when registering from the Event Organizer website.
    /// Role can be "Public" (regular user) or "Organizer" (can create events).
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new { message = "Email already in use." });

        if (request.Role != "Public" && request.Role != "Organizer")
            return BadRequest(new { message = "Role must be 'Public' or 'Organizer'." });

        if (request.Source != "KMC" && request.Source != "Organizer")
            return BadRequest(new { message = "Source must be 'KMC' or 'Organizer'." });

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            Source = request.Source
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Name, user.Email, user.Role, user.Source));
    }

    /// <summary>Login — same endpoint for both KMC and Organizer sites</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Name, user.Email, user.Role, user.Source));
    }
}