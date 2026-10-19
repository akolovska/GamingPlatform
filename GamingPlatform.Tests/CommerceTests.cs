using GamingPlatform.Domain.Enum;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    // Cart, gifts and orders. Only free orders here: paid ones need Stripe (see StripeWebhookTests).
    [Collection("Database")]
    public class CommerceTests
    {
        private const string Ok = "https://site/ok", Cancel = "https://site/cancel";

        private readonly TestDatabase _db;
        public CommerceTests(TestDatabase db) => _db = db;

        [Fact]
        public async Task Owned_and_delisted_games_cant_go_in_the_cart()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var cart = sp.GetRequiredService<ICartService>();
            var player = await TestData.UserAsync(sp);
            var owned = TestData.Game(sp);
            TestData.Own(sp, player.Id, owned.Id);

            Assert.Equal(ResultStatus.Invalid, cart.Add(player.Id, owned.Id).Status);
            Assert.Equal(ResultStatus.NotFound, cart.Add(player.Id, TestData.Game(sp, delisted: true).Id).Status);
        }

        [Fact]
        public async Task The_cart_uses_todays_prices_and_drops_games_that_were_delisted()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var cart = sp.GetRequiredService<ICartService>();
            var player = await TestData.UserAsync(sp);
            var onSale = TestData.Game(sp, price: 40, discountPercent: 25);
            var later = TestData.Game(sp, price: 10);
            cart.Add(player.Id, onSale.Id);
            cart.Add(player.Id, later.Id);

            later.IsDelisted = true;
            sp.GetRequiredService<ApplicationDbContext>().SaveChanges();

            var items = cart.GetCart(player.Id).Items;
            Assert.Single(items);
            Assert.Equal(30m, items[0].Price);
            Assert.Equal(1, cart.Count(player.Id));
        }

        [Fact]
        public async Task Checking_out_a_free_cart_fills_the_library_and_empties_the_cart()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var player = await TestData.UserAsync(sp);
            var a = TestData.Game(sp, price: 0);
            var b = TestData.Game(sp, price: 0);
            var cart = sp.GetRequiredService<ICartService>();
            cart.Add(player.Id, a.Id);
            cart.Add(player.Id, b.Id);

            var result = await sp.GetRequiredService<IOrderService>().CheckoutCartAsync(player.Id, Ok, Cancel);

            Assert.True(result.Succeeded);
            Assert.Null(result.Value);
            using var check = _db.NewScope();
            var db = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(2, db.Libraries.Count(l => l.GamerId == player.Id));
            Assert.Equal(0, db.CartItems.Count(c => c.UserId == player.Id));
            var order = db.Orders.Single(o => o.BuyerId == player.Id);
            Assert.Equal(OrderStatus.Paid, order.Status);
            Assert.Single(sp.GetRequiredService<IOrderService>().GetOrders(player.Id));
        }

        [Fact]
        public async Task Gifts_only_go_to_friends()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var buyer = await TestData.UserAsync(sp);
            var stranger = await TestData.UserAsync(sp);
            var game = TestData.Game(sp, price: 0);

            var result = await sp.GetRequiredService<IOrderService>().GiftAsync(buyer.Id, game.Id, stranger.Id, Ok, Cancel);

            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Equal(ResultStatus.Invalid, (await sp.GetRequiredService<IOrderService>().GiftAsync(buyer.Id, game.Id, buyer.Id, Ok, Cancel)).Status);
        }

        [Fact]
        public async Task A_gift_lands_in_the_friends_library_with_a_notification()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var buyer = await TestData.UserAsync(sp);
            var friend = await TestData.UserAsync(sp);
            TestData.MakeFriends(sp, buyer, friend);
            var game = TestData.Game(sp, price: 0);
            var orders = sp.GetRequiredService<IOrderService>();

            Assert.True((await orders.GiftAsync(buyer.Id, game.Id, friend.Id, Ok, Cancel)).Succeeded);
            var again = await orders.GiftAsync(buyer.Id, game.Id, friend.Id, Ok, Cancel);

            Assert.Contains("already owns", again.Message);
            using var check = _db.NewScope();
            var db = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.True(db.Libraries.Any(l => l.GamerId == friend.Id && l.GameId == game.Id));
            Assert.False(db.Libraries.Any(l => l.GamerId == buyer.Id && l.GameId == game.Id));
            Assert.True(db.Notifications.Any(n => n.UserId == friend.Id && n.Message.Contains(buyer.UserName!)));
            Assert.Equal(friend.UserName, orders.GetOrders(buyer.Id).Single().Items.Single().GiftedTo);
        }
    }
}
