using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TactiqAPI.Data;
using TactiqAPI.DTOs;
using TactiqAPI.Models;
using TactiqAPI.Services;

namespace TactiqAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly TactiqDbContext _context;
    private readonly IPlaystyleService _playstyleService;

    public MatchesController(TactiqDbContext context, IPlaystyleService playstyleService)
    {
        _context = context;
        _playstyleService = playstyleService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MatchDto>>> GetMatches()
    {
        var userId = GetCurrentUserId();

        var matches = await BuildMatchDtoQuery(userId)
            .OrderByDescending(m => m.MatchDate)
            .ToListAsync();

        return Ok(matches);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MatchDto>> GetMatch(int id)
    {
        var userId = GetCurrentUserId();
        
        var match = await BuildMatchDtoQuery(userId)
            .Where(m => m.Id == id)
            .FirstOrDefaultAsync();

        if (match is null)
            return NotFound(new { message = "Maç bulunamadı." });

        return Ok(match);
    }

    private IQueryable<MatchDto> BuildMatchDtoQuery(int userId)
    {
        return _context.Matches
            .Where(m => m.CreatedByUserId == userId)
            .Select(m => new MatchDto
            {
                Id = m.Id,
                MatchDate = m.MatchDate,
                Duration = m.Duration,
                HomeScore = m.HomeScore,
                AwayScore = m.AwayScore,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                Players = m.MatchPlayers
                    .OrderBy(mp => mp.Team)
                    .ThenBy(mp => mp.Player!.Name)
                    .Select(mp => new MatchPlayerDto
                    {
                        PlayerId = mp.PlayerId,
                        PlayerName = mp.Player!.Name,
                        Position = mp.Player.Position,
                        Team = mp.Team,
                        Stats = _context.PlayerStats
                            .Where(s => s.MatchId == m.Id && s.PlayerId == mp.PlayerId)
                            .Select(s => new PlayerStatsDto
                            {
                                Goals = s.Goals,
                                Assists = s.Assists,
                                ShotsOnTarget = s.ShotsOnTarget,
                                SuccessfulPasses = s.SuccessfulPasses,
                                Tackles = s.Tackles,
                                Saves = s.Saves,
                                Rating = s.Rating
                            })
                            .FirstOrDefault()
                    })
                    .ToList()
            });
    }

    [HttpPost]
    public async Task<ActionResult<MatchDto>> CreateMatch([FromBody] CreateMatchRequest request)
    {
        var userId = GetCurrentUserId();
        var validationError = await ValidateMatchRequest(request.Duration, request.HomeScore, request.AwayScore, request.Players, userId);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
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
            await RefreshPlayerCards(userId, request.Players.Select(player => player.PlayerId));

            await transaction.CommitAsync();

            var createdMatch = await BuildMatchDtoQuery(userId)
                .FirstAsync(created => created.Id == match.Id);

            return CreatedAtAction(nameof(GetMatch), new { id = match.Id }, createdMatch);
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Maç kaydedilirken bir hata oluştu.");
        }
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<MatchDto>> CreateBulkMatch([FromBody] CreateBulkMatchRequest request)
    {
        var players = request.HomePlayerIds
            .Select(id => new MatchPlayerRequest { PlayerId = id, Team = "Home", Stats = EmptyStats(request.DefaultRating) })
            .Concat(request.AwayPlayerIds.Select(id => new MatchPlayerRequest { PlayerId = id, Team = "Away", Stats = EmptyStats(request.DefaultRating) }))
            .ToList();

        return await CreateMatch(new CreateMatchRequest
        {
            MatchDate = request.MatchDate,
            Duration = request.Duration,
            HomeScore = request.HomeScore,
            AwayScore = request.AwayScore,
            Players = players
        });
    }

    [HttpPost("seed-recent")]
    public async Task<IActionResult> SeedRecentMatches()
    {
        var userId = GetCurrentUserId();
        var matches = await _context.Matches
            .Where(match => match.CreatedByUserId == userId)
            .Include(match => match.MatchPlayers)
            .Include(match => match.Statistics)
            .OrderByDescending(match => match.MatchDate)
            .ToListAsync();

        if (matches.Count == 0)
            return BadRequest(new { message = "Once en az bir mac kaydetmelisin." });

        var missingCount = Math.Max(0, 5 - matches.Count);
        if (missingCount == 0)
            return Ok(new { createdCount = 0, totalMatches = matches.Count });

        var template = matches.First();
        var playerIds = template.MatchPlayers.Select(player => player.PlayerId).ToList();

        for (var index = 1; index <= missingCount; index++)
        {
            var match = new Match
            {
                MatchDate = template.MatchDate.AddDays(-7 * index),
                Duration = template.Duration,
                HomeScore = Math.Max(0, template.HomeScore + ((index % 3) - 1)),
                AwayScore = Math.Max(0, template.AwayScore + (((index + 1) % 3) - 1)),
                CreatedByUserId = userId
            };

            foreach (var player in template.MatchPlayers)
            {
                match.MatchPlayers.Add(new MatchPlayer
                {
                    PlayerId = player.PlayerId,
                    Team = player.Team
                });
            }

            foreach (var stat in template.Statistics)
            {
                match.Statistics.Add(new PlayerStats
                {
                    PlayerId = stat.PlayerId,
                    Goals = Vary(stat.Goals, index % 2),
                    Assists = Vary(stat.Assists, (index + 1) % 2),
                    ShotsOnTarget = Vary(stat.ShotsOnTarget, (index % 3) - 1),
                    SuccessfulPasses = Vary(stat.SuccessfulPasses, index - 2),
                    Tackles = Vary(stat.Tackles, (index % 3) - 1),
                    Saves = Vary(stat.Saves, index % 2),
                    Rating = Math.Clamp(Math.Round(stat.Rating + (((index % 3) - 1) * 0.3), 1), 1, 10)
                });
            }

            _context.Matches.Add(match);
        }

        await _context.SaveChangesAsync();
        await RefreshPlayerCards(userId, playerIds);

        return Ok(new { createdCount = missingCount, totalMatches = matches.Count + missingCount });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MatchDto>> UpdateMatch(int id, [FromBody] UpdateMatchRequest request)
    {
        var userId = GetCurrentUserId();
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .Include(m => m.Statistics)
            .FirstOrDefaultAsync(m => m.Id == id && m.CreatedByUserId == userId);

        if (match is null)
            return NotFound(new { message = "Maç bulunamadı." });

        var validationError = await ValidateMatchRequest(request.Duration, request.HomeScore, request.AwayScore, request.Players, userId);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            match.MatchDate = NormalizeDate(request.MatchDate);
            match.Duration = request.Duration;
            match.HomeScore = request.HomeScore;
            match.AwayScore = request.AwayScore;
            match.UpdatedAt = DateTime.UtcNow;

            _context.MatchPlayers.RemoveRange(match.MatchPlayers);
            _context.PlayerStats.RemoveRange(match.Statistics);
            
            AddPlayersAndStats(match, request.Players);

            await _context.SaveChangesAsync();
            await RefreshPlayerCards(userId, request.Players.Select(player => player.PlayerId));

            await transaction.CommitAsync();
            return Ok(new { message = "Maç başarıyla güncellendi." });
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Maç güncellenirken bir hata oluştu.");
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMatch(int id)
    {
        var userId = GetCurrentUserId();
        var match = await _context.Matches
            .FirstOrDefaultAsync(m => m.Id == id && m.CreatedByUserId == userId);

        if (match is null)
            return NotFound(new { message = "Maç bulunamadı." });

        var playerIds = await _context.MatchPlayers
            .Where(matchPlayer => matchPlayer.MatchId == id)
            .Select(matchPlayer => matchPlayer.PlayerId)
            .ToListAsync();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Matches.Remove(match);
            await _context.SaveChangesAsync();
            
            await RefreshPlayerCards(userId, playerIds);

            await transaction.CommitAsync();
            return NoContent();
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Maç silinirken bir hata oluştu.");
        }
    }

    private async Task RefreshPlayerCards(int userId, IEnumerable<int> playerIds)
    {
        var ids = playerIds.Distinct().ToList();
        
        var players = await _context.Players
            .Where(player => player.CreatedByUserId == userId && ids.Contains(player.Id))
            .ToListAsync();

        foreach (var player in players)
        {
            var recentRatings = await _context.PlayerStats
                .Include(s => s.Match)
                .Where(s => s.PlayerId == player.Id)
                .OrderByDescending(s => s.Match!.MatchDate)
                .Select(s => s.Rating)
                .Take(5)
                .ToListAsync();

            var ratingAverage = recentRatings.DefaultIfEmpty(5).Average();

            player.Form = Math.Clamp((int)Math.Round(ratingAverage * 10), 1, 99);
            player.Overall = Math.Clamp((int)Math.Round((player.Overall * 0.7) + (player.Form * 0.3)), 1, 99);
            player.PrimaryPlaystyle = player.Playstyles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()
                ?? _playstyleService.Analyze(player).Label;
            player.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
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
        if (duration <= 0) return "Maç süresi 0'dan büyük olmalıdır.";
        if (homeScore < 0 || awayScore < 0) return "Skor değerleri negatif olamaz.";
        if (players.Count == 0) return "Maça en az bir oyuncu eklenmelidir.";
        if (players.Any(p => !IsValidTeam(p.Team))) return "Takım değeri Home veya Away olmalıdır.";
        if (players.Any(p => HasNegativeStats(p.Stats))) return "Oyuncu istatistikleri negatif olamaz.";

        var playerIds = players.Select(p => p.PlayerId).ToList();
        if (playerIds.Count != playerIds.Distinct().Count()) return "Aynı oyuncu maça birden fazla eklenemez.";

        var ownedPlayerCount = await _context.Players
            .CountAsync(p => p.CreatedByUserId == userId && playerIds.Contains(p.Id));

        if (ownedPlayerCount != playerIds.Count) return "Maça yalnızca size ait oyuncular eklenebilir.";

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
                || stats.Saves < 0
                || stats.Rating is < 1 or > 10);
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

            if (player.Stats is null) continue;

            match.Statistics.Add(new PlayerStats
            {
                Match = match,
                PlayerId = player.PlayerId,
                Goals = player.Stats.Goals,
                Assists = player.Stats.Assists,
                ShotsOnTarget = player.Stats.ShotsOnTarget,
                SuccessfulPasses = player.Stats.SuccessfulPasses,
                Tackles = player.Stats.Tackles,
                Saves = player.Stats.Saves,
                Rating = player.Stats.Rating
            });
        }
    }

    private static PlayerStatsRequest EmptyStats(double rating)
    {
        return new PlayerStatsRequest { Rating = rating };
    }

    private static int Vary(int value, int delta)
    {
        return Math.Max(0, value + delta);
    }

    private static DateTime NormalizeDate(DateTime date)
    {
        return date.Kind == DateTimeKind.Utc ? date : DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    private static bool IsValidTeam(string team)
    {
        return string.Equals(team, "Home", StringComparison.OrdinalIgnoreCase)
            || string.Equals(team, "Away", StringComparison.OrdinalIgnoreCase)
            || string.Equals(team, "A", StringComparison.OrdinalIgnoreCase)
            || string.Equals(team, "B", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTeam(string team)
    {
        var value = team.Trim();
        if (string.Equals(value, "A", StringComparison.OrdinalIgnoreCase))
            return "Home";

        if (string.Equals(value, "B", StringComparison.OrdinalIgnoreCase))
            return "Away";

        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}
