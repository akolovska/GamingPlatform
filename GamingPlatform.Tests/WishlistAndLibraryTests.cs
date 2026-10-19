using GamingPlatform.Repository.Data;
using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    [Collection("Database")]
    public class WishlistAndLibraryTests
    {
        private readonly TestDatabase _db;
        public WishlistAndLibraryTests(TestDatabase db) => _db = db;

        [Fact]
        public async Task Delisted_games_cant_be_wishlisted()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var player = await TestData.UserAsync(sp);
            var game = TestData.Game(sp, delisted: true);

            Assert.Equal(ResultStatus.NotFound, sp.GetRequiredService<IWishlistService>().Add(player.Id, game.Id).Status);
        }

        [Fact]
        public async Task Owned_games_cant_be_wishlisted()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var player = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            TestData.Own(sp, player.Id, game.Id);

            Assert.Equal(ResultStatus.Invalid, sp.GetRequiredService<IWishlistService>().Add(player.Id, game.Id).Status);
        }

        [Fact]
        public async Task Adding_twice_keeps_one_row()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var wishlist = sp.GetRequiredService<IWishlistService>();
            var player = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);

            Assert.True(wishlist.Add(player.Id, game.Id).Succeeded);
            Assert.True(wishlist.Add(player.Id, game.Id).Succeeded); // a double click is harmless

            Assert.Equal(1, sp.GetRequiredService<ApplicationDbContext>().Wishlists.Count(w => w.GamerId == player.Id));
        }

        [Fact]
        public async Task A_delisted_game_disappears_from_the_wishlist_instead_of_leaving_a_dead_card()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var wishlist = sp.GetRequiredService<IWishlistService>();
            var player = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            wishlist.Add(player.Id, game.Id);

            game.IsDelisted = true;
            sp.GetRequiredService<ApplicationDbContext>().SaveChanges();

            Assert.Empty(wishlist.GetMyWishlist(player.Id));
        }

        [Fact]
        public async Task Getting_a_free_game_adds_it_once_and_removes_it_from_the_wishlist()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var orders = sp.GetRequiredService<IOrderService>();
            var player = await TestData.UserAsync(sp);
            var game = TestData.Game(sp, price: 0);
            sp.GetRequiredService<IWishlistService>().Add(player.Id, game.Id);

            var first = await orders.BuyNowAsync(player.Id, game.Id, "https://x/ok", "https://x/cancel");
            var second = await orders.BuyNowAsync(player.Id, game.Id, "https://x/ok", "https://x/cancel");

            Assert.True(first.Succeeded);
            Assert.Null(first.Value);   // no Stripe redirect for free games
            Assert.Contains("already own", second.Message);
            var db = sp.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, db.Libraries.Count(l => l.GamerId == player.Id));
            Assert.Equal(0, db.Wishlists.Count(w => w.GamerId == player.Id));
        }

        [Fact]
        public async Task Delisted_games_cant_be_bought()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var player = await TestData.UserAsync(sp);
            var game = TestData.Game(sp, price: 0, delisted: true);

            var result = await sp.GetRequiredService<IOrderService>().BuyNowAsync(player.Id, game.Id, "https://x/ok", "https://x/cancel");

            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Contains("no longer for sale", result.Message);
        }
    }
}
