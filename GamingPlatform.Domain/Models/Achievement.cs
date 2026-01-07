namespace GamingPlatform.Domain.Models;

public class Achievement : BaseEntity
{
    public Guid? GamerId { get; set; }
    public Gamer? Gamer { get; set; }
    public Guid? GameId { get; set; }
    public Game? Game { get; set; }
    public string? Title { get; set; }
    public DateTime? DateReceived { get; set; }

}