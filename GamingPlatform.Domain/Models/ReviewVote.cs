namespace GamingPlatform.Domain.Models
{
    // "Was this review helpful?" One vote per user per review.
    public class ReviewVote : BaseEntity
    {
        public Guid ReviewId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool IsHelpful { get; set; }   // true = upvote, false = downvote
    }
}
