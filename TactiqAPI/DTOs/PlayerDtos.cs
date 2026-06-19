namespace TactiqAPI.DTOs;

public class CreatePlayerRequest
{
    public required string Name { get; set; }
    public required string Position { get; set; }
    public required string StrongFoot { get; set; }
    public double? Height { get; set; }
    public double? Weight { get; set; }
}

public class UpdatePlayerRequest
{
    public required string Name { get; set; }
    public required string Position { get; set; }
    public required string StrongFoot { get; set; }
    public double? Height { get; set; }
    public double? Weight { get; set; }
}

public class PlayerDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Position { get; set; }
    public required string StrongFoot { get; set; }
    public double? Height { get; set; }
    public double? Weight { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
