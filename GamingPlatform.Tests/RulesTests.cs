using GamingPlatform.Domain.Models;
using GamingPlatform.Service;

namespace GamingPlatform.Tests
{
    // The small pure rules: prices, review labels, trailer links. No database.
    public class RulesTests
    {
        private static readonly DateTime Now = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData(20.00, 0, null, 20.00)]      // no sale
        [InlineData(20.00, 25, null, 15.00)]     // open-ended sale
        [InlineData(19.99, 30, 1, 13.99)]        // 13.993 rounds to 13.99
        [InlineData(20.00, 25, -1, 20.00)]       // the sale already ended
        public void Current_price(double price, int discount, int? endsInDays, double expected)
        {
            var game = new Game { Price = (decimal)price, DiscountPercent = discount, DiscountEndsAt = endsInDays == null ? null : Now.AddDays(endsInDays.Value) };
            Assert.Equal((decimal)expected, Pricing.CurrentPrice(game, Now));
        }

        [Theory]
        [InlineData(-1, true)]   // started yesterday
        [InlineData(0, true)]    // starts right now
        [InlineData(2, false)]   // scheduled: not on sale yet
        public void Sales_can_be_scheduled(int startsInDays, bool onSale)
        {
            var game = new Game { Price = 20, DiscountPercent = 50, DiscountStartsAt = Now.AddDays(startsInDays), DiscountEndsAt = Now.AddDays(5) };
            Assert.Equal(onSale, Pricing.IsOnSale(game, Now));
            Assert.Equal(onSale ? 10m : 20m, Pricing.CurrentPrice(game, Now));
            Assert.True(Pricing.HasCurrentOrUpcomingSale(game, Now));
        }

        [Theory]
        [InlineData(0, 0, "No user reviews")]
        [InlineData(3, 3, "Positive")]                 // 100%, few reviews
        [InlineData(45, 50, "Very Positive")]          // 90% of 50
        [InlineData(490, 500, "Overwhelmingly Positive")]
        [InlineData(399, 500, "Mostly Positive")]      // 79.8% rounds down: not "Positive"
        [InlineData(5, 10, "Mixed")]
        [InlineData(3, 10, "Mostly Negative")]
        [InlineData(1, 10, "Negative")]
        [InlineData(5, 50, "Very Negative")]
        [InlineData(10, 600, "Overwhelmingly Negative")]
        public void Steam_style_review_labels(int positive, int total, string expected) =>
            Assert.Equal(expected, ReviewSummary.Label(positive, total));

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
        [InlineData("https://youtu.be/dQw4w9WgXcQ?t=42", "dQw4w9WgXcQ")]
        [InlineData("youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
        [InlineData("https://m.youtube.com/watch?feature=share&v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
        [InlineData("https://evil.example/watch?v=dQw4w9WgXcQ", null)]
        [InlineData("javascript:alert(1)//youtu.be/dQw4w9WgXcQ", null)]
        [InlineData("https://www.youtube.com/watch?v=short", null)]
        public void Trailer_links_must_be_youtube(string url, string? videoId) =>
            Assert.Equal(videoId, YouTube.VideoId(url));
    }
}
