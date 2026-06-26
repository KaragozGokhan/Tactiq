using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TactiqAPI.Data;
using TactiqAPI.DTOs;

namespace TactiqAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly TactiqDbContext _context;

    public AiController(TactiqDbContext context)
    {
        _context = context;
    }

    [HttpPost("player-analysis/{playerId:int}")]
    public async Task<ActionResult<AiAnalysisResponse>> AnalyzePlayer(int playerId)
    {
        var userId = GetCurrentUserId();
        var player = await _context.Players
            .Include(player => player.Statistics)
                .ThenInclude(stat => stat.Match)
            .FirstOrDefaultAsync(player => player.Id == playerId && player.CreatedByUserId == userId);

        if (player is null)
            return NotFound(new { message = "Oyuncu bulunamadı." });

        var lastFive = player.Statistics
            .OrderByDescending(stat => stat.Match!.MatchDate)
            .Take(5)
            .ToList();

        var lastTen = player.Statistics
            .OrderByDescending(stat => stat.Match!.MatchDate)
            .Take(10)
            .ToList();

        var averageRating = lastFive.Count == 0 ? 0 : Math.Round(lastFive.Average(stat => stat.Rating), 1);
        var goals = lastTen.Sum(stat => stat.Goals);
        var assists = lastTen.Sum(stat => stat.Assists);
        var tackles = lastTen.Sum(stat => stat.Tackles);
        var saves = lastTen.Sum(stat => stat.Saves);

        return Ok(new AiAnalysisResponse
        {
            Summary = $"{player.Name} son {lastFive.Count} maçta {averageRating}/10 ortalama ile oynadı. Overall {player.Overall}, form {player.Form}.",
            Highlights =
            [
                $"Son 10 maç katkısı: {goals} gol, {assists} asist, {tackles} top çalma, {saves} kurtarış.",
                $"Ana playstyle: {player.PrimaryPlaystyle}."
            ],
            Suggestions =
            [
                averageRating >= 7.5 ? "Formu yüksek; kadro kurarken güçlü sinyal olarak kullanılabilir." : "Formu orta/düşük; maç içi rating girildikçe daha sağlıklı denge verir.",
                goals + assists > tackles + saves ? "Hücum katkısı öne çıkıyor." : "Savunma/denge katkısı öne çıkıyor."
            ]
        });
    }

    [HttpPost("team-analysis")]
    public ActionResult<AiAnalysisResponse> AnalyzeTeam([FromBody] TeamAnalysisRequest request)
    {
        var scoreDifference = Math.Round(Math.Abs(request.TeamAScore - request.TeamBScore), 1);

        return Ok(new AiAnalysisResponse
        {
            Summary = $"{request.TeamSize}v{request.TeamSize} kadro {request.BalancePercentage}% denge ile kuruldu.",
            Highlights =
            [
                $"Diziliş: {request.Formation ?? "serbest"}.",
                $"Takım güç farkı: {scoreDifference}."
            ],
            Suggestions =
            [
                request.BalancePercentage >= 95 ? "Kadro dengesi çok iyi; bu dağılım kullanılabilir." : "Denge düşükse bir yüksek overall oyuncuyu karşı takıma kaydırmayı dene.",
                "Maç sonrası rating ve istatistik girildikçe sonraki kadro önerisi güçlenir."
            ]
        });
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var id)
            ? id
            : throw new UnauthorizedAccessException("Kullanıcı kimliği okunamadı.");
    }
}
