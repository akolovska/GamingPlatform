namespace GamingPlatform.Domain.Models;

public class Review : BaseEntity
{
    public string? GamerId { get; set; }
    public Guid? GameId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime? DateRated { get; set; }
}