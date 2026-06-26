namespace TactiqAPI.DTOs;

public class PlayerPlaystyleDto
{
    public int PlayerId { get; set; }
    public required string PlayerName { get; set; }
    public required string Label { get; set; }
    public required string AnalysisLabel { get; set; }
    public required string PrimaryPlaystyle { get; set; }
    public List<string> Playstyles { get; set; } = [];
    public int MatchesAnalyzed { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int ShotsOnTarget { get; set; }
    public int SuccessfulPasses { get; set; }
    public int Tackles { get; set; }
    public int Saves { get; set; }
}
