using GamingPlatform.Domain.DTO;
using GamingPlatform.Service;

namespace GamingPlatform.Web.Helpers
{
    /// <summary>What one game card shows, and which page it links to.</summary>
    public class GameCardModel
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = "";
        public string? Cover { get; init; }
        public List<string> Genres { get; init; } = new();
        public string? Platform { get; init; }
        public bool IsFree { get; init; }
        public decimal? Price { get; init; }
        public decimal CurrentPrice { get; init; }
        public int DiscountPercent { get; init; }
        public bool IsComingSoon { get; init; }
        public string ReviewSummary { get; init; } = "";
        public int ReviewCount { get; init; }
        public bool Owned { get; init; }
        public string Controller { get; init; } = "Games";

        public string ReviewTone => GamingPlatform.Service.ReviewSummary.Tone(ReviewSummary);

        // controller: "Games" links to the store page, "Libraries" to the owned-game page
        public static GameCardModel From(GameDto g, string controller = "Games") => new()
        {
            Id = g.Id,
            Title = g.Title,
            Cover = g.CoverImageUrl,
            Genres = g.Genres,
            Platform = g.Platform,
            IsFree = g.IsFree,
            Price = g.Price,
            CurrentPrice = g.CurrentPrice,
            DiscountPercent = g.DiscountPercent,
            IsComingSoon = g.IsComingSoon,
            ReviewSummary = g.ReviewSummary,
            ReviewCount = g.ReviewCount,
            Owned = g.InLibrary && controller == "Games", // the ribbon is noise on the library page itself
            Controller = controller
        };
    }
}
