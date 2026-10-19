namespace GamingPlatform.Domain.DTO
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public string? Comment { get; set; }
        public int Score { get; set; }
        public DateTime? DateRated { get; set; }
        public string GamerName { get; set; } = string.Empty;
        public bool IsMine { get; set; }      // only the author can edit
        public bool CanDelete { get; set; }   // the author or an admin

        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        public bool? MyVote { get; set; }     // true = I upvoted, false = I downvoted, null = no vote
        public bool CanVote { get; set; }     // signed in, and not my own review
    }
}
