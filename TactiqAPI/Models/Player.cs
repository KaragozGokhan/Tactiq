namespace TactiqAPI.Models;

public class Player
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public required string Position { get; set; }
    public required string StrongFoot { get; set; }
    public double? Height { get; set; }
    public double? Weight { get; set; }

    public int Overall { get; set; } = 50;
    public int Form { get; set; } = 50;
    public string PrimaryPlaystyle { get; set; } = "Dengeli Oyuncu";
    public string Playstyles { get; set; } = string.Empty;

    public int Pace { get; set; } = 50;
    public int Shoot { get; set; } = 50;
    public int Pass { get; set; } = 50;
    public int Dribbling { get; set; } = 50;
    public int Def { get; set; } = 50;
    public int Phy { get; set; } = 50;

    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User? CreatedByUser { get; set; }
    public ICollection<MatchPlayer> MatchPlayers { get; set; } = [];
    public ICollection<PlayerStats> Statistics { get; set; } = [];
}