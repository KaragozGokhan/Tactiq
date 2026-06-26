using TactiqAPI.DTOs;
using TactiqAPI.Models;

namespace TactiqAPI.Services;

public interface ITeamBuilderService
{
    BuildTeamsResponse BuildBalancedTeams(IReadOnlyList<Player> players, int teamSize, string? formation);
}

public class TeamBuilderService : ITeamBuilderService
{
    public BuildTeamsResponse BuildBalancedTeams(IReadOnlyList<Player> players, int teamSize, string? formation)
    {
        var requiredPlayerCount = teamSize * 2;
        if (players.Count != requiredPlayerCount)
            throw new ArgumentException($"Takim olusturmak icin tam {requiredPlayerCount} oyuncu secilmelidir.", nameof(players));

        var scoredPlayers = players
            .Select(player => new ScoredPlayer(player, CalculatePowerScore(player)))
            .OrderByDescending(player => player.PowerScore)
            .ThenBy(player => player.Player.Name)
            .ToList();

        var bestResult = default(TeamCandidate);
        var totalScore = scoredPlayers.Sum(player => player.PowerScore);
        var targetPositionCounts = GetTargetPositionCounts(teamSize, formation);

        // ponytail: exhaustive combinations are fine up to 11v11; switch to heuristic if bigger formats arrive.
        foreach (var teamAIndexes in GetTeamCombinations(requiredPlayerCount, teamSize))
        {
            var candidateTeamAIndexes = teamAIndexes.ToHashSet();
            var teamAPlayers = scoredPlayers.Where((_, index) => candidateTeamAIndexes.Contains(index)).ToList();
            var teamBPlayers = scoredPlayers.Where((_, index) => !candidateTeamAIndexes.Contains(index)).ToList();
            var teamAScore = teamAIndexes.Sum(index => scoredPlayers[index].PowerScore);
            var teamBScore = totalScore - teamAScore;
            var scoreDifference = Math.Abs(teamAScore - teamBScore);
            var positionPenalty = CalculatePositionPenalty(teamAPlayers, targetPositionCounts)
                + CalculatePositionPenalty(teamBPlayers, targetPositionCounts)
                + CalculateRoleDifferencePenalty(teamAPlayers, teamBPlayers);
            var compositeScore = (positionPenalty * 1000) + scoreDifference;

            if (bestResult is not null && compositeScore >= bestResult.CompositeScore)
                continue;

            bestResult = new TeamCandidate(teamAIndexes, positionPenalty, compositeScore);
        }

        if (bestResult is null)
            throw new ArgumentException("Secilen oyuncular iki denk kadroya ayrilamadi.");

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
            TeamSize = teamSize,
            Formation = formation,
            BalancePercentage = CalculateBalancePercentage(finalTeamAScore, finalTeamBScore),
            PositionBalancePenalty = bestResult.PositionPenalty,
            TeamAScore = Math.Round(finalTeamAScore, 2),
            TeamBScore = Math.Round(finalTeamBScore, 2),
            TeamA = teamA.Select(ToDto).ToList(),
            TeamB = teamB.Select(ToDto).ToList()
        };
    }

    private static int CalculatePositionPenalty(List<ScoredPlayer> players, Dictionary<string, int> targetPositionCounts)
    {
        var selectedCounts = CountRoles(players);

        var penalty = targetPositionCounts.Sum(target =>
            Math.Abs(selectedCounts.GetValueOrDefault(target.Key) - target.Value));
        penalty += selectedCounts
            .Where(count => !targetPositionCounts.ContainsKey(count.Key))
            .Sum(count => count.Value * 2);
        return penalty;
    }

    private static int CalculateRoleDifferencePenalty(List<ScoredPlayer> teamA, List<ScoredPlayer> teamB)
    {
        var teamACounts = CountRoles(teamA);
        var teamBCounts = CountRoles(teamB);
        return teamACounts.Keys
            .Union(teamBCounts.Keys)
            .Sum(role => Math.Abs(teamACounts.GetValueOrDefault(role) - teamBCounts.GetValueOrDefault(role)));
    }

    private static Dictionary<string, int> CountRoles(List<ScoredPlayer> players)
    {
        return players
            .GroupBy(player => NormalizePositionGroup(player.Player.Position))
            .ToDictionary(group => group.Key, group => group.Count());
    }

    private static Dictionary<string, int> GetTargetPositionCounts(int teamSize, string? formation)
    {
        int[] rows = string.IsNullOrWhiteSpace(formation)
            ? [Math.Max(1, teamSize - 5), 3, 1]
            : formation.Split(['-', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToArray();

        return new Dictionary<string, int>
        {
            ["Goalkeeper"] = 1,
            ["Defender"] = rows.FirstOrDefault(),
            ["Midfielder"] = rows.Skip(1).SkipLast(1).Sum(),
            ["Forward"] = rows.Length > 1 ? rows.Last() : 0
        };
    }

    private static double CalculatePowerScore(Player player)
    {
        var stats = player.Statistics
            .OrderByDescending(stat => stat.Match?.MatchDate ?? DateTime.MinValue)
            .Take(10)
            .ToList();
        var matchCount = Math.Max(stats.Count, 1);
        var lastFive = stats.Take(5).ToList();
        var recentRating = lastFive.Count == 0 ? player.Form : lastFive.Average(stat => stat.Rating) * 10;
        var trend = CalculateRatingTrend(lastFive);

        var attackingScore = stats.Sum(stat =>
            (stat.Goals * 4.0)
            + (stat.Assists * 3.0)
            + (stat.ShotsOnTarget * 1.5));

        var teamPlayScore = stats.Sum(stat => stat.SuccessfulPasses * 0.08);
        var defensiveScore = stats.Sum(stat => (stat.Tackles * 2.0) + (stat.Saves * 2.5));
        var statsBonus = Math.Min(14, GetPositionImpact(player.Position, attackingScore, teamPlayScore, defensiveScore) / matchCount);
        var cardScore = (player.Overall * 0.45) + (recentRating * 0.35) + (player.Form * 0.20);

        return Math.Round(cardScore + trend + GetPositionScore(player.Position) + GetPhysicalScore(player) + statsBonus, 2);
    }

    private static double CalculateRatingTrend(List<PlayerStats> lastFive)
    {
        if (lastFive.Count < 4)
            return 0;

        var latest = lastFive.Take(2).Average(stat => stat.Rating);
        var previous = lastFive.Skip(2).Take(3).Average(stat => stat.Rating);
        return Math.Clamp((latest - previous) * 3, -4, 4);
    }

    private static double GetPositionImpact(string position, double attackingScore, double teamPlayScore, double defensiveScore)
    {
        return NormalizePositionGroup(position) switch
        {
            "Goalkeeper" => defensiveScore,
            "Defender" => (defensiveScore * 0.7) + (teamPlayScore * 0.3),
            "Midfielder" => (teamPlayScore * 0.55) + (attackingScore * 0.25) + (defensiveScore * 0.2),
            "Forward" => (attackingScore * 0.8) + (teamPlayScore * 0.2),
            _ => attackingScore + teamPlayScore + defensiveScore
        };
    }

    private static double GetPositionScore(string position)
    {
        return NormalizePositionGroup(position) switch
        {
            "Goalkeeper" => 5,
            "Defender" => 4,
            "Midfielder" => 4.5,
            "Forward" => 4,
            _ => 3
        };
    }

    private static string NormalizePositionGroup(string position)
    {
        var value = position.Trim().ToLowerInvariant();

        if (value.Contains("kaleci") || value.Contains("keeper") || value == "gk")
            return "Goalkeeper";

        if (value.Contains("defans") || value.Contains("defender") || value.Contains("back") || value is "cb" or "lb" or "rb")
            return "Defender";

        if (value.Contains("orta") || value.Contains("mid") || value is "cm" or "dm" or "am")
            return "Midfielder";

        if (value.Contains("forvet") || value.Contains("forward") || value.Contains("striker") || value.Contains("wing") || value is "st" or "lw" or "rw")
            return "Forward";

        return "Other";
    }

    private static double GetPhysicalScore(Player player)
    {
        var heightScore = player.Height.HasValue ? Math.Clamp((player.Height.Value - 165) / 10, 0, 3) : 0;
        var weightScore = player.Weight.HasValue ? Math.Clamp((player.Weight.Value - 60) / 20, 0, 2) : 0;

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

    private sealed record TeamCandidate(List<int> TeamAIndexes, int PositionPenalty, double CompositeScore);
}
