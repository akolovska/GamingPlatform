using System.Linq.Expressions;
using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service
{
    // The one place that decides what a game costs today
    public static class Pricing
    {
        public const int MaxDiscountPercent = 90;

        // On sale = a discount, it has started, and it hasn't ended
        public static bool IsOnSale(Game game, DateTime utcNow) =>
            game.DiscountPercent > 0 &&
            (game.DiscountStartsAt == null || game.DiscountStartsAt <= utcNow) &&
            (game.DiscountEndsAt == null || game.DiscountEndsAt > utcNow);

        // A sale that is running now or will start later (what the Edit form shows)
        public static bool HasCurrentOrUpcomingSale(Game game, DateTime utcNow) =>
            game.DiscountPercent > 0 && (game.DiscountEndsAt == null || game.DiscountEndsAt > utcNow);

        public static decimal CurrentPrice(Game game, DateTime utcNow)
        {
            var full = game.Price ?? 0;
            if (full <= 0 || !IsOnSale(game, utcNow)) return Math.Max(full, 0);
            return Math.Round(full * (100 - game.DiscountPercent) / 100m, 2, MidpointRounding.AwayFromZero);
        }

        // The same rules written so EF can translate them to SQL (store filter, sorting, the sale job)
        public static Expression<Func<Game, bool>> OnSaleInSql(DateTime utcNow) =>
            g => (g.Price ?? 0) > 0 && g.DiscountPercent > 0 &&
                 (g.DiscountStartsAt == null || g.DiscountStartsAt <= utcNow) &&
                 (g.DiscountEndsAt == null || g.DiscountEndsAt > utcNow);

        public static Expression<Func<Game, decimal>> CurrentPriceInSql(DateTime utcNow) =>
            g => g.DiscountPercent > 0 &&
                 (g.DiscountStartsAt == null || g.DiscountStartsAt <= utcNow) &&
                 (g.DiscountEndsAt == null || g.DiscountEndsAt > utcNow)
                ? (g.Price ?? 0) * (100 - g.DiscountPercent) / 100
                : (g.Price ?? 0);
    }
}
