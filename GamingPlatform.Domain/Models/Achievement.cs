namespace GamingPlatform.Domain.Models;

public class Achievement : BaseEntity
{
    public Guid? GameId { get; set; }
    public Game? Game { get; set; }
    public string? Title { get; set; }
}