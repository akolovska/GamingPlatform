using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;

namespace GamingPlatform.Web.Helpers
{
    /// <summary>Lightweight view model so one card partial can render both Game entities and GameDtos.</summary>
    public class GameCardModel
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = "";
        public string? Cover { get; init; }
        public string? Genre { get; init; }
        public string? Platform { get; init; }
        public decimal? Price { get; init; }
        public DateTime? ReleaseDate { get; init; }
        public bool Owned { get; init; }
        public string Controller { get; init; } = "Games";
        public string Action { get; init; } = "Details";

        public static GameCardModel From(Game g) => new()
        {
            Id = g.Id, Title = g.Title ?? "", Cover = g.CoverImageUrl, Genre = g.GenreNames,
            Platform = g.Platform, Price = g.Price, ReleaseDate = g.ReleaseDate
        };

        public static GameCardModel From(GameDto g, string controller = "Games", bool owned = false) => new()
        {
            Id = g.Id, Title = g.Title ?? "", Cover = g.CoverImageUrl, Genre = g.Genre,
            Platform = g.Platform, Price = g.Price, ReleaseDate = g.ReleaseDate,
            Controller = controller, Owned = owned
        };
    }
}
