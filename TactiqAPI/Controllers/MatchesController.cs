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
public class MatchesController : ControllerBase
{
    private readonly TactiqDbContext _context;

    public MatchesController(TactiqDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MatchDto>>> GetMatches()
    {
        var userId = GetCurrentUserId();

        var matches = await _context.Matches
            .Where(m => m.CreatedByUserId == userId)
            .Include(m => m.MatchPlayers)
                .ThenInclude(mp => mp.Player)
            .Include(m => m.Statistics)
            .OrderByDescending(m => m.MatchDate)
            .ToListAsync();

        return Ok(matches.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MatchDto>> GetMatch(int id)
    {
        var userId = GetCurrentUserId();
        var match = await GetUserMatchQuery(userId)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match is null)
            return NotFound(new { message = "Maç bulunamadı." });

        return Ok(ToDto(match));
    }

    [HttpPost]
    public async Task<ActionResult<MatchDto>> CreateMatch([FromBody] CreateMatchRequest request)
    {
        var userId = GetCurrentUserId();
        var validationError = await ValidateMatchRequest(request.Duration, request.HomeScore, request.AwayScore, request.Players, userId);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var match = new Match
        {
            MatchDate = NormalizeDate(request.MatchDate),
            Duration = request.Duration,
            HomeScore = request.HomeScore,
            AwayScore = request.AwayScore,
            CreatedByUserId = userId
        };

        AddPlayersAndStats(match, request.Players);

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var createdMatch = await GetUserMatchQuery(userId)
            .FirstAsync(m => m.Id == match.Id);

        return CreatedAtAction(nameof(GetMatch), new { id = match.Id }, ToDto(createdMatch));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MatchDto>> UpdateMatch(int id, [FromBody] UpdateMatchRequest request)
    {
        var userId = GetCurrentUserId();
        var match = await GetUserMatchQuery(userId)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match is null)
            return NotFound(new { message = "Maç bulunamadı." });

        var validationError = await ValidateMatchRequest(request.Duration, request.HomeScore, request.AwayScore, request.Players, userId);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        match.MatchDate = NormalizeDate(request.MatchDate);
        match.Duration = request.Duration;
        match.HomeScore = request.HomeScore;
        match.AwayScore = request.AwayScore;
        match.UpdatedAt = DateTime.UtcNow;

        _context.MatchPlayers.RemoveRange(match.MatchPlayers);
        _context.PlayerStats.RemoveRange(match.Statistics);
        AddPlayersAndStats(match, request.Players);

        await _context.SaveChangesAsync();

        var updatedMatch = await GetUserMatchQuery(userId)
            .FirstAsync(m => m.Id == match.Id);

        return Ok(ToDto(updatedMatch));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMatch(int id)
    {
        var userId = GetCurrentUserId();
        var match = await _context.Matches
            .FirstOrDefaultAsync(m => m.Id == id && m.CreatedByUserId == userId);

        if (match is null)
            return NotFound(new { message = "Maç bulunamadı." });

        _context.Matches.Remove(match);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private IQueryable<Match> GetUserMatchQuery(int userId)
    {
        return _context.Matches
            .Where(m => m.CreatedByUserId == userId)
            .Include(m => m.MatchPlayers)
                .ThenInclude(mp => mp.Player)
            .Include(m => m.Statistics);
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var id)
            ? id
            : throw new UnauthorizedAccessException("Kullanıcı kimliği okunamadı.");
    }

    private async Task<string?> ValidateMatchRequest(int duration, int homeScore, int awayScore, List<MatchPlayerRequest> players, int userId)
    {
        if (duration <= 0)
            return "Maç süresi 0'dan büyük olmalıdır.";

        if (homeScore < 0 || awayScore < 0)
            return "Skor değerleri negatif olamaz.";

        if (players.Count == 0)
            return "Maça en az bir oyuncu eklenmelidir.";

        if (players.Any(p => !IsValidTeam(p.Team)))
            return "Takım değeri Home veya Away olmalıdır.";

        if (players.Any(p => HasNegativeStats(p.Stats)))
            return "Oyuncu istatistikleri negatif olamaz.";

        var playerIds = players.Select(p => p.PlayerId).ToList();
        if (playerIds.Count != playerIds.Distinct().Count())
            return "Aynı oyuncu maça birden fazla eklenemez.";

        var ownedPlayerCount = await _context.Players
            .CountAsync(p => p.CreatedByUserId == userId && playerIds.Contains(p.Id));

        if (ownedPlayerCount != playerIds.Count)
            return "Maça yalnızca size ait oyuncular eklenebilir.";

        return null;
    }

    private static bool HasNegativeStats(PlayerStatsRequest? stats)
    {
        return stats is not null
            && (stats.Goals < 0
                || stats.Assists < 0
                || stats.ShotsOnTarget < 0
                || stats.SuccessfulPasses < 0
                || stats.Tackles < 0
                || stats.Saves < 0);
    }

    private static void AddPlayersAndStats(Match match, List<MatchPlayerRequest> players)
    {
        foreach (var player in players)
        {
            match.MatchPlayers.Add(new MatchPlayer
            {
                Match = match,
                PlayerId = player.PlayerId,
                Team = NormalizeTeam(player.Team)
            });

            if (player.Stats is null)
                continue;

            match.Statistics.Add(new PlayerStats
            {
                Match = match,
                PlayerId = player.PlayerId,
                Goals = player.Stats.Goals,
                Assists = player.Stats.Assists,
                ShotsOnTarget = player.Stats.ShotsOnTarget,
                SuccessfulPasses = player.Stats.SuccessfulPasses,
                Tackles = player.Stats.Tackles,
                Saves = player.Stats.Saves
            });
        }
    }

    private static MatchDto ToDto(Match match)
    {
        return new MatchDto
        {
            Id = match.Id,
            MatchDate = match.MatchDate,
            Duration = match.Duration,
            HomeScore = match.HomeScore,
            AwayScore = match.AwayScore,
            CreatedAt = match.CreatedAt,
            UpdatedAt = match.UpdatedAt,
            Players = match.MatchPlayers
                .OrderBy(mp => mp.Team)
                .ThenBy(mp => mp.Player!.Name)
                .Select(mp =>
                {
                    var stats = match.Statistics.FirstOrDefault(s => s.PlayerId == mp.PlayerId);
                    return new MatchPlayerDto
                    {
                        PlayerId = mp.PlayerId,
                        PlayerName = mp.Player!.Name,
                        Position = mp.Player.Position,
                        Team = mp.Team,
                        Stats = stats is null ? null : new PlayerStatsDto
                        {
                            Goals = stats.Goals,
                            Assists = stats.Assists,
                            ShotsOnTarget = stats.ShotsOnTarget,
                            SuccessfulPasses = stats.SuccessfulPasses,
                            Tackles = stats.Tackles,
                            Saves = stats.Saves
                        }
                    };
                })
                .ToList()
        };
    }

    private static DateTime NormalizeDate(DateTime date)
    {
        return date.Kind == DateTimeKind.Utc
            ? date
            : DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    private static bool IsValidTeam(string team)
    {
        return string.Equals(team, "Home", StringComparison.OrdinalIgnoreCase)
            || string.Equals(team, "Away", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTeam(string team)
    {
        var value = team.Trim();
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}
