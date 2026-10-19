namespace GamingPlatform.Domain.DTO
{
    // The store page of one game, plus what the viewer may do on it
    public class GameDetailsDto : GameDto
    {
        public List<ReviewDto> Reviews { get; set; } = new();
        public string? TrailerEmbedUrl { get; set; }  // a youtube-nocookie.com embed URL
        public bool CanManage { get; set; }           // edit, delist, achievements, screenshots
        public bool CanReview { get; set; }           // owns it, it's released, and hasn't reviewed it yet

        // Friends the viewer can gift this game to (they don't own it yet)
        public List<UserSummaryDto> GiftableFriends { get; set; } = new();
    }
}
