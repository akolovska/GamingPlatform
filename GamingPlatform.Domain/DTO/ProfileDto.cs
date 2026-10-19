namespace GamingPlatform.Domain.DTO
{
    // A profile page. The "own profile" fields are filled only on your own profile,
    // the "someone else's" fields only on another user's.
    public class ProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public List<UserSummaryDto> Friends { get; set; } = new();
        public List<GameDto> Games { get; set; } = new();

        // Own profile
        public string? Email { get; set; }
        public bool IsVerified { get; set; }
        public List<PendingRequestDto> PendingRequests { get; set; } = new();
        public List<UserSummaryDto> BlockedUsers { get; set; } = new();

        // Someone else's profile
        public bool IsSelf { get; set; }
        public bool IsFriend { get; set; }
        public bool RequestSent { get; set; }
        public Guid? IncomingRequestId { get; set; }
        public bool CanViewWishlist { get; set; }
    }
}
