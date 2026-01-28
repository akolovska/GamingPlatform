namespace GamingPlatform.Domain.Models;

public class Achievement : BaseEntity
{
    public Guid GameId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ICollection<GamerAchievement>? GamerAchievements { get; set; }
}