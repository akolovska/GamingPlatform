namespace GamingPlatform.Domain.DTO
{
    // A user in a list (friends, blocked users)
    public class UserSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }
}
