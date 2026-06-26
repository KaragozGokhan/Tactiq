using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TactiqAPI.Data;
using TactiqAPI.DTOs;
using TactiqAPI.Models;

namespace TactiqAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly TactiqDbContext _context;

    public UsersController(TactiqDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var user = await _context.Users.FindAsync(GetCurrentUserId());
        return user is null ? NotFound(new { message = "Kullanici bulunamadi." }) : Ok(ToDto(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Kullanici adi ve email gereklidir." });

        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
            return NotFound(new { message = "Kullanici bulunamadi." });

        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var exists = await _context.Users.AnyAsync(u =>
            u.Id != userId && (u.Username == username || u.Email == email));

        if (exists)
            return BadRequest(new { message = "Bu email veya kullanici adi zaten kayitlidir." });

        user.Username = username;
        user.Email = email;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ToDto(user));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var user = await _context.Users.FindAsync(GetCurrentUserId());
        if (user is null)
            return NotFound(new { message = "Kullanici bulunamadi." });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.Users
            .OrderBy(u => u.Username)
            .Select(u => ToDto(u))
            .ToListAsync();

        return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        return user is null ? NotFound(new { message = "Kullanici bulunamadi." }) : Ok(ToDto(user));
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var id)
            ? id
            : throw new UnauthorizedAccessException("Kullanici kimligi okunamadi.");
    }

    private static UserDto ToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role
        };
    }
}
