namespace TactiqAPI.DTOs;

public class CreatePlayerRequest
{
    public required string Name { get; set; }
    public required string Position { get; set; }
    public required string StrongFoot { get; set; }
    public double? Height { get; set; }
    public double? Weight { get; set; }
    public int? Overall { get; set; }
    public int? Form { get; set; }
    public string? PrimaryPlaystyle { get; set; }
    public List<string>? Playstyles { get; set; }
    public int? Pace { get; set; }
    public int? Shoot { get; set; }
    public int? Pass { get; set; }
    public int? Dribbling { get; set; }
    public int? Def { get; set; }
    public int? Phy { get; set; }
}

public class UpdatePlayerRequest
{
    public required string Name { get; set; }
    public required string Position { get; set; }
    public required string StrongFoot { get; set; }
    public double? Height { get; set; }
    public double? Weight { get; set; }
    public int? Overall { get; set; }
    public int? Form { get; set; }
    public string? PrimaryPlaystyle { get; set; }
    public List<string>? Playstyles { get; set; }
    public int? Pace { get; set; }
    public int? Shoot { get; set; }
    public int? Pass { get; set; }
    public int? Dribbling { get; set; }
    public int? Def { get; set; }
    public int? Phy { get; set; }
}

public class PlayerDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Position { get; set; }
    public required string StrongFoot { get; set; }
    public double? Height { get; set; }
    public double? Weight { get; set; }
    public int Overall { get; set; }
    public int Form { get; set; }
    public required string PrimaryPlaystyle { get; set; }
    public List<string> Playstyles { get; set; } = [];
    public int Pace { get; set; }
    public int Shoot { get; set; }
    public int Pass { get; set; }
    public int Dribbling { get; set; }
    public int Def { get; set; }
    public int Phy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class BulkDeletePlayersRequest
{
    public List<int> PlayerIds { get; set; } = [];
}
