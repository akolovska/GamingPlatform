namespace GamingPlatform.Domain.Models;

public class Rating : BaseEntity
{
    public Guid? GamerId { get; set; }
    public Gamer? Gamer { get; set; }
    public Guid? GameId { get; set; }
    public Game? Game { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime? DateRated { get; set; }
}