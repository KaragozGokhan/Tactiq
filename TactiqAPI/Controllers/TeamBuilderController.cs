using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TactiqAPI.Data;
using TactiqAPI.DTOs;
using TactiqAPI.Services;

namespace TactiqAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/team-builder")]
public class TeamBuilderController : ControllerBase
{
    private const int MinTeamSize = 6;
    private const int MaxTeamSize = 11;

    private readonly TactiqDbContext _context;
    private readonly ITeamBuilderService _teamBuilderService;

    public TeamBuilderController(TactiqDbContext context, ITeamBuilderService teamBuilderService)
    {
        _context = context;
        _teamBuilderService = teamBuilderService;
    }

    [HttpPost("balance")]
    public async Task<ActionResult<BuildTeamsResponse>> BuildBalancedTeams([FromBody] BuildTeamsRequest request)
    {
        if (request.TeamSize is < MinTeamSize or > MaxTeamSize)
            return BadRequest(new { message = "Takim boyutu 6 ile 11 arasinda olmalidir." });

        var requiredPlayerCount = request.TeamSize * 2;
        if (request.PlayerIds.Count != requiredPlayerCount)
            return BadRequest(new { message = $"Takim olusturmak icin tam {requiredPlayerCount} oyuncu secilmelidir." });

        if (request.PlayerIds.Count != request.PlayerIds.Distinct().Count())
            return BadRequest(new { message = "Ayni oyuncu birden fazla secilemez." });

        if (!IsValidFormation(request.Formation, request.TeamSize))
            return BadRequest(new { message = "Dizilis ornegi: 2-3-1. Toplam, kaleci haric takim boyutundan 1 eksik olmalidir." });

        var userId = GetCurrentUserId();
        var players = await _context.Players
            .Where(player => player.CreatedByUserId == userId && request.PlayerIds.Contains(player.Id))
            .Include(player => player.Statistics)
                .ThenInclude(stat => stat.Match)
            .ToListAsync();

        if (players.Count != requiredPlayerCount)
            return BadRequest(new { message = $"Takim olusturmak icin yalnizca size ait {requiredPlayerCount} oyuncu secebilirsiniz." });

        try
        {
            return Ok(_teamBuilderService.BuildBalancedTeams(players, request.TeamSize, request.Formation));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static bool IsValidFormation(string? formation, int teamSize)
    {
        if (string.IsNullOrWhiteSpace(formation))
            return true;

        var lines = formation.Split(['-', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Length > 0
            && lines.All(line => int.TryParse(line, out var count) && count >= 0)
            && lines.Sum(int.Parse) == teamSize - 1;
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var id)
            ? id
            : throw new UnauthorizedAccessException("Kullanici kimligi okunamadi.");
    }
}
