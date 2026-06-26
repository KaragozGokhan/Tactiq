namespace TactiqAPI.DTOs;

public class BuildTeamsRequest
{
    public int TeamSize { get; set; } = 7;
    public string? Formation { get; set; }
    public List<int> PlayerIds { get; set; } = [];
}

public class BuildTeamsResponse
{
    public int TeamSize { get; set; }
    public string? Formation { get; set; }
    public double BalancePercentage { get; set; }
    public int PositionBalancePenalty { get; set; }
    public double TeamAScore { get; set; }
    public double TeamBScore { get; set; }
    public List<TeamBuilderPlayerDto> TeamA { get; set; } = [];
    public List<TeamBuilderPlayerDto> TeamB { get; set; } = [];
}

public class TeamBuilderPlayerDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Position { get; set; }
    public required string StrongFoot { get; set; }
    public double PowerScore { get; set; }
}
