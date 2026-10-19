namespace GamingPlatform.Domain.DTO
{
    // Someone else's wishlist. Games is empty when IsVisible is false.
    public class WishlistDto
    {
        public string OwnerName { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public List<GameDto> Games { get; set; } = new();
    }
}
