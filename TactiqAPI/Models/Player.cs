namespace TactiqAPI.Models;

public class Player
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Position { get; set; }
    public required string StrongFoot { get; set; } // "Right", "Left", "Both"
    public double? Height { get; set; } // cm
    public double? Weight { get; set; } // kg
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Relations
    public User? CreatedByUser { get; set; }
    public ICollection<MatchPlayer> MatchPlayers { get; set; } = [];
    public ICollection<PlayerStats> Statistics { get; set; } = [];
}
