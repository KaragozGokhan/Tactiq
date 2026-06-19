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
    private const int RequiredPlayerCount = 14;

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
        if (request.PlayerIds.Count != RequiredPlayerCount)
            return BadRequest(new { message = "Takım oluşturmak için tam 14 oyuncu seçilmelidir." });

        if (request.PlayerIds.Count != request.PlayerIds.Distinct().Count())
            return BadRequest(new { message = "Aynı oyuncu birden fazla seçilemez." });

        var userId = GetCurrentUserId();
        var players = await _context.Players
            .Where(player => player.CreatedByUserId == userId && request.PlayerIds.Contains(player.Id))
            .Include(player => player.Statistics)
                .ThenInclude(stat => stat.Match)
            .ToListAsync();

        if (players.Count != RequiredPlayerCount)
            return BadRequest(new { message = "Takım oluşturmak için yalnızca size ait 14 oyuncu seçebilirsiniz." });

        return Ok(_teamBuilderService.BuildBalancedTeams(players));
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var id)
            ? id
            : throw new UnauthorizedAccessException("Kullanıcı kimliği okunamadı.");
    }
}
