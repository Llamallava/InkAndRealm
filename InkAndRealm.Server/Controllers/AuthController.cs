using InkAndRealm.Server.Data;
using InkAndRealm.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace InkAndRealm.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const int SessionDays = 14;
    private readonly DemoMapContext _context;

    public AuthController(DemoMapContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var trimmedUsername = (request.Username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedUsername) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required.");
        }

        if (request.Password.Length < 6)
        {
            return BadRequest("Password must be at least 6 characters.");
        }

        var normalized = trimmedUsername.ToUpperInvariant();
        var exists = await _context.Users.AnyAsync(user => user.UsernameNormalized == normalized);
        if (exists)
        {
            return Conflict("Username is already taken.");
        }

        var userEntity = new UserEntity
        {
            Username = trimmedUsername,
            UsernameNormalized = normalized,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _context.Users.Add(userEntity);
        await _context.SaveChangesAsync();

        var session = await CreateSessionAsync(userEntity.Id);

        return Ok(new AuthResponse
        {
            UserId = userEntity.Id,
            Username = userEntity.Username,
            SessionToken = session.Token
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var trimmedUsername = (request.Username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedUsername) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required.");
        }

        var normalized = trimmedUsername.ToUpperInvariant();
        var userEntity = await _context.Users.FirstOrDefaultAsync(user => user.UsernameNormalized == normalized);
        if (userEntity is null)
        {
            return Unauthorized("Invalid username or password.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, userEntity.PasswordHash))
        {
            return Unauthorized("Invalid username or password.");
        }

        var session = await CreateSessionAsync(userEntity.Id);

        return Ok(new AuthResponse
        {
            UserId = userEntity.Id,
            Username = userEntity.Username,
            SessionToken = session.Token
        });
    }

    private async Task<SessionEntity> CreateSessionAsync(int userId)
    {
        var session = new SessionEntity
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString("N"),
            CreatedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddDays(SessionDays)
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }
}
