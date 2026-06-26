using TactiqAPI.DTOs;
using TactiqAPI.Models;

namespace TactiqAPI.Services;

public interface IPlaystyleService
{
    PlayerPlaystyleDto Analyze(Player player);
}

public class PlaystyleService : IPlaystyleService
{
    public PlayerPlaystyleDto Analyze(Player player)
    {
        var lastStats = player.Statistics
            .OrderByDescending(stat => stat.Match?.MatchDate ?? DateTime.MinValue)
            .Take(10)
            .ToList();

        var goals = lastStats.Sum(stat => stat.Goals);
        var assists = lastStats.Sum(stat => stat.Assists);
        var shotsOnTarget = lastStats.Sum(stat => stat.ShotsOnTarget);
        var successfulPasses = lastStats.Sum(stat => stat.SuccessfulPasses);
        var tackles = lastStats.Sum(stat => stat.Tackles);
        var saves = lastStats.Sum(stat => stat.Saves);

        var analysisLabel = GetPlaystyleLabel(player.Position, goals, assists, shotsOnTarget, successfulPasses, tackles, saves);
        var playstyles = SplitPlaystyles(player.Playstyles);

        return new PlayerPlaystyleDto
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            Label = string.IsNullOrWhiteSpace(player.PrimaryPlaystyle) ? analysisLabel : player.PrimaryPlaystyle,
            AnalysisLabel = analysisLabel,
            PrimaryPlaystyle = player.PrimaryPlaystyle,
            Playstyles = playstyles,
            MatchesAnalyzed = lastStats.Count,
            Goals = goals,
            Assists = assists,
            ShotsOnTarget = shotsOnTarget,
            SuccessfulPasses = successfulPasses,
            Tackles = tackles,
            Saves = saves
        };
    }

    private static string GetPlaystyleLabel(
        string position,
        int goals,
        int assists,
        int shotsOnTarget,
        int successfulPasses,
        int tackles,
        int saves)
    {
        var normalizedPosition = position.Trim().ToLowerInvariant();

        if ((normalizedPosition.Contains("kaleci") || normalizedPosition.Contains("keeper") || normalizedPosition == "gk")
            && saves >= 15)
            return "Refleks Kaleci";

        if (goals >= 20 && goals >= assists * 2)
            return "Bitirici Forvet";

        if (assists >= 15 && assists > goals)
            return "Oyun Kurucu";

        if (successfulPasses >= 180 && assists >= 8)
            return "Pas İstasyonu";

        if (tackles >= 25 && tackles > goals + assists)
            return "Top Kazanan";

        if (shotsOnTarget >= 25 && goals >= 10)
            return "Şut Tehdidi";

        if (goals >= 8 && assists >= 8)
            return "Çift Yönlü Hücumcu";

        return "Dengeli Oyuncu";
    }

    private static List<string> SplitPlaystyles(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
