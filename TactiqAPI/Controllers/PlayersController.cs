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

    // DÜZELTİLEN KISIM: Varsayılan POST metodu artık doğrudan liste (array) kabul ediyor.
    // Tekli endpoint kaldırıldı, Swagger'da direkt 16 kişiyi buraya yapıştırabilirsin.
    [HttpPost]
    public async Task<ActionResult<IEnumerable<PlayerDto>>> CreatePlayers([FromBody] List<CreatePlayerRequest> requests)
    {
        if (requests == null || requests.Count == 0)
            return BadRequest(new { message = "En az bir oyuncu gönderilmelidir." });

        var userId = GetCurrentUserId();
        var players = new List<Player>();

        foreach (var request in requests)
        {
            var validationError = ValidatePlayerRequest(request.Name, request.Position, request.StrongFoot, request.Height, request.Weight);
            if (validationError is not null)
                // Hangi oyuncuda hata olduğunu görmek için ismini de mesaja ekledik
                return BadRequest(new { message = $"{request.Name ?? "Bilinmeyen Oyuncu"}: {validationError}" });

            var playstyles = request.Playstyles ?? [];
            var playstyleError = ValidatePlaystyles(playstyles);
            if (playstyleError is not null)
                return BadRequest(new { message = $"{request.Name ?? "Bilinmeyen Oyuncu"}: {playstyleError}" });

            var pace = ClampScore(request.Pace ?? 50);
            var shoot = ClampScore(request.Shoot ?? 50);
            var pass = ClampScore(request.Pass ?? 50);
            var dribbling = ClampScore(request.Dribbling ?? 50);
            var def = ClampScore(request.Def ?? 50);
            var phy = ClampScore(request.Phy ?? 50);

            players.Add(new Player
            {
                Name = request.Name.Trim(),
                Position = request.Position.Trim(),
                StrongFoot = NormalizeStrongFoot(request.StrongFoot),
                Height = request.Height,
                Weight = request.Weight,
                Overall = request.Overall.HasValue
                    ? ClampScore(request.Overall.Value)
                    : CalculateOverall(pace, shoot, pass, dribbling, def, phy),
                Form = ClampScore(request.Form ?? 50),
                PrimaryPlaystyle = string.IsNullOrWhiteSpace(request.PrimaryPlaystyle)
                    ? playstyles.FirstOrDefault() ?? "Dengeli Oyuncu"
                    : request.PrimaryPlaystyle.Trim(),
                Playstyles = JoinPlaystyles(playstyles),
                Pace = pace,
                Shoot = shoot,
                Pass = pass,
                Dribbling = dribbling,
                Def = def,
                Phy = phy,
                CreatedByUserId = userId
            });
        }

        try
        {
            _context.Players.AddRange(players);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
        }

        return Ok(players.Select(ToDto));
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

        var playstyles = request.Playstyles;
        var playstyleError = ValidatePlaystyles(playstyles ?? []);
        if (playstyleError is not null)
            return BadRequest(new { message = playstyleError });

        player.Pace = ClampScore(request.Pace ?? player.Pace);
        player.Shoot = ClampScore(request.Shoot ?? player.Shoot);
        player.Pass = ClampScore(request.Pass ?? player.Pass);
        player.Dribbling = ClampScore(request.Dribbling ?? player.Dribbling);
        player.Def = ClampScore(request.Def ?? player.Def);
        player.Phy = ClampScore(request.Phy ?? player.Phy);

        player.Name = request.Name.Trim();
        player.Position = request.Position.Trim();
        player.StrongFoot = NormalizeStrongFoot(request.StrongFoot);
        player.Height = request.Height;
        player.Weight = request.Weight;
        player.Overall = request.Overall.HasValue
            ? ClampScore(request.Overall.Value)
            : CalculateOverall(player.Pace, player.Shoot, player.Pass, player.Dribbling, player.Def, player.Phy);
        player.Form = ClampScore(request.Form ?? player.Form);
        player.PrimaryPlaystyle = string.IsNullOrWhiteSpace(request.PrimaryPlaystyle)
            ? playstyles?.FirstOrDefault() ?? player.PrimaryPlaystyle
            : request.PrimaryPlaystyle.Trim();
        if (playstyles is not null)
            player.Playstyles = JoinPlaystyles(playstyles);
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

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> DeletePlayers([FromBody] BulkDeletePlayersRequest request)
    {
        if (request.PlayerIds.Count == 0)
            return BadRequest(new { message = "En az bir oyuncu id gönderilmelidir." });

        var userId = GetCurrentUserId();
        var players = await _context.Players
            .Where(player => player.CreatedByUserId == userId && request.PlayerIds.Contains(player.Id))
            .ToListAsync();

        _context.Players.RemoveRange(players);
        await _context.SaveChangesAsync();

        return Ok(new { deletedCount = players.Count });
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
            Overall = player.Overall,
            Form = player.Form,
            PrimaryPlaystyle = player.PrimaryPlaystyle,
            Playstyles = SplitPlaystyles(player.Playstyles),
            Pace = player.Pace,
            Shoot = player.Shoot,
            Pass = player.Pass,
            Dribbling = player.Dribbling,
            Def = player.Def,
            Phy = player.Phy,
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

    private static int ClampScore(int value)
    {
        return Math.Clamp(value, 1, 99);
    }

    private static int CalculateOverall(params int[] values)
    {
        return ClampScore((int)Math.Round(values.Average()));
    }

    private static List<string> SplitPlaystyles(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static string? ValidatePlaystyles(List<string> playstyles)
    {
        return playstyles.Any(string.IsNullOrWhiteSpace) ? "Playstyle bos olamaz." : null;
    }

    private static string JoinPlaystyles(List<string> playstyles)
    {
        return string.Join(",", playstyles.Select(playstyle => playstyle.Trim()));
    }
}
