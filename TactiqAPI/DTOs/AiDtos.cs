namespace TactiqAPI.DTOs;

public class TeamAnalysisRequest
{
    public int TeamSize { get; set; }
    public string? Formation { get; set; }
    public double BalancePercentage { get; set; }
    public double TeamAScore { get; set; }
    public double TeamBScore { get; set; }
    public List<TeamBuilderPlayerDto> TeamA { get; set; } = [];
    public List<TeamBuilderPlayerDto> TeamB { get; set; } = [];
}

public class AiAnalysisResponse
{
    public required string Summary { get; set; }
    public List<string> Highlights { get; set; } = [];
    public List<string> Suggestions { get; set; } = [];
}
