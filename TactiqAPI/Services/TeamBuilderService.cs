using TactiqAPI.DTOs;
using TactiqAPI.Models;

namespace TactiqAPI.Services;

public interface ITeamBuilderService
{
    BuildTeamsResponse BuildBalancedTeams(IReadOnlyList<Player> players);
}

public class TeamBuilderService : ITeamBuilderService
{
    private const int RequiredPlayerCount = 14;
    private const int TeamSize = 7;

    public BuildTeamsResponse BuildBalancedTeams(IReadOnlyList<Player> players)
    {
        if (players.Count != RequiredPlayerCount)
            throw new ArgumentException("Takım oluşturmak için tam 14 oyuncu seçilmelidir.", nameof(players));

        var scoredPlayers = players
            .Select(player => new ScoredPlayer(player, CalculatePowerScore(player)))
            .OrderByDescending(player => player.PowerScore)
            .ThenBy(player => player.Player.Name)
            .ToList();

        var bestResult = default(TeamCandidate);
        var totalScore = scoredPlayers.Sum(player => player.PowerScore);
        var totalPositionCounts = scoredPlayers
            .GroupBy(player => NormalizePositionGroup(player.Player.Position))
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var teamAIndexes in GetTeamCombinations(RequiredPlayerCount, TeamSize))
        {
            var teamAScore = teamAIndexes.Sum(index => scoredPlayers[index].PowerScore);
            var teamBScore = totalScore - teamAScore;
            var scoreDifference = Math.Abs(teamAScore - teamBScore);
            var positionPenalty = CalculatePositionPenalty(teamAIndexes, scoredPlayers, totalPositionCounts);
            var compositeScore = (positionPenalty * 1000) + scoreDifference;

            if (bestResult is not null && compositeScore >= bestResult.CompositeScore)
                continue;

            bestResult = new TeamCandidate(teamAIndexes, scoreDifference, positionPenalty, compositeScore);
        }

        if (bestResult is null)
            throw new InvalidOperationException("Takım kombinasyonu oluşturulamadı.");

        var teamAIndexSet = bestResult.TeamAIndexes.ToHashSet();
        var teamA = scoredPlayers
            .Where((_, index) => teamAIndexSet.Contains(index))
            .OrderByDescending(player => player.PowerScore)
            .ToList();
        var teamB = scoredPlayers
            .Where((_, index) => !teamAIndexSet.Contains(index))
            .OrderByDescending(player => player.PowerScore)
            .ToList();

        var finalTeamAScore = teamA.Sum(player => player.PowerScore);
        var finalTeamBScore = teamB.Sum(player => player.PowerScore);

        return new BuildTeamsResponse
        {
            BalancePercentage = CalculateBalancePercentage(finalTeamAScore, finalTeamBScore),
            PositionBalancePenalty = bestResult.PositionPenalty,
            TeamAScore = Math.Round(finalTeamAScore, 2),
            TeamBScore = Math.Round(finalTeamBScore, 2),
            TeamA = teamA.Select(ToDto).ToList(),
            TeamB = teamB.Select(ToDto).ToList()
        };
    }

    private static int CalculatePositionPenalty(
        List<int> teamAIndexes,
        List<ScoredPlayer> scoredPlayers,
        Dictionary<string, int> totalPositionCounts)
    {
        var teamAIndexSet = teamAIndexes.ToHashSet();
        var teamAPositionCounts = scoredPlayers
            .Where((_, index) => teamAIndexSet.Contains(index))
            .GroupBy(player => NormalizePositionGroup(player.Player.Position))
            .ToDictionary(group => group.Key, group => group.Count());

        var penalty = 0;
        foreach (var (position, totalCount) in totalPositionCounts)
        {
            var teamACount = teamAPositionCounts.GetValueOrDefault(position);
            var idealLow = totalCount / 2;
            var idealHigh = (int)Math.Ceiling(totalCount / 2.0);

            if (teamACount < idealLow)
                penalty += idealLow - teamACount;

            if (teamACount > idealHigh)
                penalty += teamACount - idealHigh;
        }

        return penalty;
    }

    private static double CalculatePowerScore(Player player)
    {
        var stats = player.Statistics
            .OrderByDescending(stat => stat.Match?.MatchDate ?? DateTime.MinValue)
            .Take(10)
            .ToList();
        var matchCount = Math.Max(stats.Count, 1);

        var attackingScore = stats.Sum(stat =>
            (stat.Goals * 4.0)
            + (stat.Assists * 3.0)
            + (stat.ShotsOnTarget * 1.5));

        var teamPlayScore = stats.Sum(stat =>
            stat.SuccessfulPasses * 0.08);

        var defensiveScore = stats.Sum(stat =>
            (stat.Tackles * 2.0)
            + (stat.Saves * 2.5));

        var averageStatsScore = (attackingScore + teamPlayScore + defensiveScore) / matchCount;
        var positionScore = GetPositionScore(player.Position);
        var physicalScore = GetPhysicalScore(player);

        return Math.Round(50 + positionScore + physicalScore + averageStatsScore, 2);
    }

    private static double GetPositionScore(string position)
    {
        var normalizedPosition = NormalizePositionGroup(position);

        if (normalizedPosition == "Goalkeeper")
            return 5;

        if (normalizedPosition == "Defender")
            return 4;

        if (normalizedPosition == "Midfielder")
            return 4.5;

        if (normalizedPosition == "Forward")
            return 4;

        return 3;
    }

    private static string NormalizePositionGroup(string position)
    {
        var normalizedPosition = position.Trim().ToLowerInvariant();

        if (normalizedPosition.Contains("kaleci")
            || normalizedPosition.Contains("keeper")
            || normalizedPosition == "gk")
            return "Goalkeeper";

        if (normalizedPosition.Contains("defans")
            || normalizedPosition.Contains("defender")
            || normalizedPosition.Contains("back")
            || normalizedPosition == "cb"
            || normalizedPosition == "lb"
            || normalizedPosition == "rb")
            return "Defender";

        if (normalizedPosition.Contains("orta")
            || normalizedPosition.Contains("mid")
            || normalizedPosition == "cm"
            || normalizedPosition == "dm"
            || normalizedPosition == "am")
            return "Midfielder";

        if (normalizedPosition.Contains("forvet")
            || normalizedPosition.Contains("forward")
            || normalizedPosition.Contains("striker")
            || normalizedPosition.Contains("wing")
            || normalizedPosition == "st"
            || normalizedPosition == "lw"
            || normalizedPosition == "rw")
            return "Forward";

        return "Other";
    }

    private static double GetPhysicalScore(Player player)
    {
        var heightScore = player.Height.HasValue
            ? Math.Clamp((player.Height.Value - 165) / 10, 0, 3)
            : 0;

        var weightScore = player.Weight.HasValue
            ? Math.Clamp((player.Weight.Value - 60) / 20, 0, 2)
            : 0;

        return heightScore + weightScore;
    }

    private static double CalculateBalancePercentage(double teamAScore, double teamBScore)
    {
        var strongerTeamScore = Math.Max(teamAScore, teamBScore);
        if (strongerTeamScore <= 0)
            return 100;

        var difference = Math.Abs(teamAScore - teamBScore);
        return Math.Round(Math.Max(0, 100 - (difference / strongerTeamScore * 100)), 2);
    }

    private static TeamBuilderPlayerDto ToDto(ScoredPlayer scoredPlayer)
    {
        return new TeamBuilderPlayerDto
        {
            Id = scoredPlayer.Player.Id,
            Name = scoredPlayer.Player.Name,
            Position = scoredPlayer.Player.Position,
            StrongFoot = scoredPlayer.Player.StrongFoot,
            PowerScore = scoredPlayer.PowerScore
        };
    }

    private static IEnumerable<List<int>> GetTeamCombinations(int playerCount, int teamSize)
    {
        var combination = new int[teamSize];

        foreach (var result in BuildCombination(0, 0))
            yield return result;

        IEnumerable<List<int>> BuildCombination(int startIndex, int depth)
        {
            if (depth == teamSize)
            {
                yield return combination.ToList();
                yield break;
            }

            for (var index = startIndex; index <= playerCount - (teamSize - depth); index++)
            {
                combination[depth] = index;

                foreach (var result in BuildCombination(index + 1, depth + 1))
                    yield return result;
            }
        }
    }

    private sealed record ScoredPlayer(Player Player, double PowerScore);

    private sealed record TeamCandidate(
        List<int> TeamAIndexes,
        double ScoreDifference,
        int PositionPenalty,
        double CompositeScore);
}
