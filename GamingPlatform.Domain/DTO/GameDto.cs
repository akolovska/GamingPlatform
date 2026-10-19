namespace GamingPlatform.Domain.DTO
{
    // A game as the store pages show it. InWishlist/InLibrary/InCart are for the person viewing it.
    public class GameDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Genres { get; set; } = new();
        public List<StudioRefDto> Developers { get; set; } = new();  // links to the studio pages
        public DateTime? ReleaseDate { get; set; }
        public string? Platform { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? SystemRequirements { get; set; }
        public List<AchievementDto> Achievements { get; set; } = new();
        public List<string> Screenshots { get; set; } = new();
        public bool IsDelisted { get; set; }

        // Price: Price is the full price, CurrentPrice what it costs today (after a sale)
        public decimal? Price { get; set; }
        public decimal CurrentPrice { get; set; }
        public int DiscountPercent { get; set; }      // 0 when not on sale
        public DateTime? DiscountEndsAt { get; set; }
        public bool IsOnSale => DiscountPercent > 0;
        public bool IsFree => (Price ?? 0) <= 0;      // no price counts as free everywhere

        public bool IsComingSoon { get; set; }        // released in the future; buying = pre-ordering

        // Reviews, summarised the way Steam does it ("Very Positive", "Mixed", ...)
        public double? Rating { get; set; }
        public int ReviewCount { get; set; }
        public string ReviewSummary { get; set; } = string.Empty;
        public int? PositivePercent { get; set; }

        public bool InWishlist { get; set; }
        public bool InLibrary { get; set; }
        public bool InCart { get; set; }
    }
}
