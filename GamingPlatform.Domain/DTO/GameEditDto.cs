namespace GamingPlatform.Domain.DTO
{
    // What the Edit game page needs besides the form itself
    public class GameEditDto
    {
        public GameFormDto Form { get; set; } = new();
        public string? CoverImageUrl { get; set; }
        public List<AchievementDto> Achievements { get; set; } = new();
        public List<string> Screenshots { get; set; } = new();
    }
}
