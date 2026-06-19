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
public class PlayersController : ControllerBase
{
    private readonly TactiqDbContext _context;

    public PlayersController(TactiqDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlayerDto>>> GetPlayers()
    {
        var userId = GetCurrentUserId();

        var players = await _context.Players
            .Where(p => p.CreatedByUserId == userId)
            .OrderBy(p => p.Name)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(players);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlayerDto>> GetPlayer(int id)
    {
        var userId = GetCurrentUserId();
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatedByUserId == userId);

        if (player is null)
            return NotFound(new { message = "Oyuncu bulunamadı." });

        return Ok(ToDto(player));
    }

    [HttpPost]
    public async Task<ActionResult<PlayerDto>> CreatePlayer([FromBody] CreatePlayerRequest request)
    {
        var validationError = ValidatePlayerRequest(request.Name, request.Position, request.StrongFoot, request.Height, request.Weight);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var player = new Player
        {
            Name = request.Name.Trim(),
            Position = request.Position.Trim(),
            StrongFoot = NormalizeStrongFoot(request.StrongFoot),
            Height = request.Height,
            Weight = request.Weight,
            CreatedByUserId = GetCurrentUserId()
        };

        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPlayer), new { id = player.Id }, ToDto(player));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlayerDto>> UpdatePlayer(int id, [FromBody] UpdatePlayerRequest request)
    {
        var userId = GetCurrentUserId();
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatedByUserId == userId);

        if (player is null)
            return NotFound(new { message = "Oyuncu bulunamadı." });

        var validationError = ValidatePlayerRequest(request.Name, request.Position, request.StrongFoot, request.Height, request.Weight);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        player.Name = request.Name.Trim();
        player.Position = request.Position.Trim();
        player.StrongFoot = NormalizeStrongFoot(request.StrongFoot);
        player.Height = request.Height;
        player.Weight = request.Weight;
        player.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ToDto(player));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePlayer(int id)
    {
        var userId = GetCurrentUserId();
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatedByUserId == userId);

        if (player is null)
            return NotFound(new { message = "Oyuncu bulunamadı." });

        _context.Players.Remove(player);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var id)
            ? id
            : throw new UnauthorizedAccessException("Kullanıcı kimliği okunamadı.");
    }

    private static PlayerDto ToDto(Player player)
    {
        return new PlayerDto
        {
            Id = player.Id,
            Name = player.Name,
            Position = player.Position,
            StrongFoot = player.StrongFoot,
            Height = player.Height,
            Weight = player.Weight,
            CreatedAt = player.CreatedAt,
            UpdatedAt = player.UpdatedAt
        };
    }

    private static string? ValidatePlayerRequest(string name, string position, string strongFoot, double? height, double? weight)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Oyuncu adı gereklidir.";

        if (string.IsNullOrWhiteSpace(position))
            return "Pozisyon gereklidir.";

        if (!IsValidStrongFoot(strongFoot))
            return "StrongFoot değeri Right, Left veya Both olmalıdır.";

        if (height is <= 0)
            return "Boy değeri 0'dan büyük olmalıdır.";

        if (weight is <= 0)
            return "Kilo değeri 0'dan büyük olmalıdır.";

        return null;
    }

    private static bool IsValidStrongFoot(string strongFoot)
    {
        return string.Equals(strongFoot, "Right", StringComparison.OrdinalIgnoreCase)
            || string.Equals(strongFoot, "Left", StringComparison.OrdinalIgnoreCase)
            || string.Equals(strongFoot, "Both", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeStrongFoot(string strongFoot)
    {
        var value = strongFoot.Trim();
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}
