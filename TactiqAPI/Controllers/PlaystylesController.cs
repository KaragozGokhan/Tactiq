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
[Route("api/playstyles")]
public class PlaystylesController : ControllerBase
{
    private readonly TactiqDbContext _context;
    private readonly IPlaystyleService _playstyleService;

    public PlaystylesController(TactiqDbContext context, IPlaystyleService playstyleService)
    {
        _context = context;
        _playstyleService = playstyleService;
    }

    [HttpGet("players")]
    public async Task<ActionResult<IEnumerable<PlayerPlaystyleDto>>> GetPlayerPlaystyles()
    {
        var userId = GetCurrentUserId();
        var players = await _context.Players
            .Where(player => player.CreatedByUserId == userId)
            .Include(player => player.Statistics)
                .ThenInclude(stat => stat.Match)
            .OrderBy(player => player.Name)
            .ToListAsync();

        return Ok(players.Select(_playstyleService.Analyze));
    }

    [HttpGet("players/{playerId:int}")]
    public async Task<ActionResult<PlayerPlaystyleDto>> GetPlayerPlaystyle(int playerId)
    {
        var userId = GetCurrentUserId();
        var player = await _context.Players
            .Where(player => player.CreatedByUserId == userId && player.Id == playerId)
            .Include(player => player.Statistics)
                .ThenInclude(stat => stat.Match)
            .FirstOrDefaultAsync();

        if (player is null)
            return NotFound(new { message = "Oyuncu bulunamadı." });

        return Ok(_playstyleService.Analyze(player));
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var id)
            ? id
            : throw new UnauthorizedAccessException("Kullanıcı kimliği okunamadı.");
    }
}
