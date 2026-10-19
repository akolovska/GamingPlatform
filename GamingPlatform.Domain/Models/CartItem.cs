namespace GamingPlatform.Domain.Models
{
    public class CartItem : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public Guid GameId { get; set; }
        public Game Game { get; set; } = null!;
        public DateTime AddedAt { get; set; }
    }
}
