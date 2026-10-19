namespace GamingPlatform.Domain.Models
{
    public class Game : BaseEntity
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? ReleaseDate { get; set; }   // in the future = "coming soon", buying = pre-ordering
        public decimal? Price { get; set; }          // the full price; no price counts as free
        public string? Platform { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? SystemRequirements { get; set; }
        public string? TrailerUrl { get; set; }      // a YouTube link

        // Screenshot image URLs, in display order. EF Core stores the list as JSON in one column.
        public List<string> Screenshots { get; set; } = new();
        public bool IsDelisted { get; set; }

        // A sale: DiscountPercent off from DiscountStartsAt until DiscountEndsAt (UTC).
        // No start = starts now; no end = until the dev ends it.
        public int DiscountPercent { get; set; }
        public DateTime? DiscountStartsAt { get; set; }
        public DateTime? DiscountEndsAt { get; set; }

        // When wishlisters were told about the current sale (null = not yet). Changing the sale resets it.
        public DateTime? SaleAnnouncedAt { get; set; }

        public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
        public ICollection<GameDev> GameDevelopers { get; set; } = new List<GameDev>();
        public ICollection<Library> Libraries { get; set; } = new List<Library>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
    }
}
