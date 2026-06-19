namespace TactiqAPI.Models;

public class Match
{
    public int Id { get; set; }
    public DateTime MatchDate { get; set; }
    public int Duration { get; set; } // minutes
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Relations
    public User? CreatedByUser { get; set; }
    public ICollection<MatchPlayer> MatchPlayers { get; set; } = [];
    public ICollection<PlayerStats> Statistics { get; set; } = [];
}
