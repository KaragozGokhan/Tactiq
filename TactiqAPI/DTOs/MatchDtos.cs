namespace TactiqAPI.DTOs;

public class CreateMatchRequest
{
    public DateTime MatchDate { get; set; }
    public int Duration { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public List<MatchPlayerRequest> Players { get; set; } = [];
}

public class UpdateMatchRequest
{
    public DateTime MatchDate { get; set; }
    public int Duration { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public List<MatchPlayerRequest> Players { get; set; } = [];
}

public class MatchPlayerRequest
{
    public int PlayerId { get; set; }
    public string Team { get; set; } = "Home";
    public PlayerStatsRequest? Stats { get; set; }
}

public class PlayerStatsRequest
{
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int ShotsOnTarget { get; set; }
    public int SuccessfulPasses { get; set; }
    public int Tackles { get; set; }
    public int Saves { get; set; }
}

public class MatchDto
{
    public int Id { get; set; }
    public DateTime MatchDate { get; set; }
    public int Duration { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<MatchPlayerDto> Players { get; set; } = [];
}

public class MatchPlayerDto
{
    public int PlayerId { get; set; }
    public required string PlayerName { get; set; }
    public required string Position { get; set; }
    public required string Team { get; set; }
    public PlayerStatsDto? Stats { get; set; }
}

public class PlayerStatsDto
{
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int ShotsOnTarget { get; set; }
    public int SuccessfulPasses { get; set; }
    public int Tackles { get; set; }
    public int Saves { get; set; }
}
