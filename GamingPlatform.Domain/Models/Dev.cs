using GamingPlatform.Domain.Identity;

namespace GamingPlatform.Domain.Models
{
    public class Dev : BaseEntity
    {
        public string? StudioName { get; set; }

        // The account that manages this studio (null = not linked to any account)
        public string? UserId { get; set; }
        public GamingPlatformUser? User { get; set; }

        public ICollection<GameDev> GameDevs { get; set; } = new List<GameDev>();
    }
}