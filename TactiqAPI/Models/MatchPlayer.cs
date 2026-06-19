namespace TactiqAPI.Models;

public class MatchPlayer
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int PlayerId { get; set; }
    public string Team { get; set; } = "Home"; // "Home" or "Away"

    // Relations
    public Match? Match { get; set; }
    public Player? Player { get; set; }
}
