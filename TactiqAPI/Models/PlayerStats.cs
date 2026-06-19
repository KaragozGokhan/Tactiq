namespace TactiqAPI.Models;

public class PlayerStats
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int PlayerId { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int ShotsOnTarget { get; set; }
    public int SuccessfulPasses { get; set; }
    public int Tackles { get; set; }
    public int Saves { get; set; }

    // Relations
    public Match? Match { get; set; }
    public Player? Player { get; set; }
}
