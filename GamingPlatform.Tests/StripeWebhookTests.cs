using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GamingPlatform.Domain.Enum;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    [Collection("Database")]
    public class StripeWebhookTests
    {
        private readonly TestDatabase _db;
        public StripeWebhookTests(TestDatabase db) => _db = db;

        // An event shaped like the ones Stripe sends
        private static string Event(string type, object dataObject) =>
            JsonSerializer.Serialize(new
            {
                id = "evt_" + Guid.NewGuid().ToString("N"),
                @object = "event",
                api_version = "2024-06-20",
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                livemode = false,
                pending_webhooks = 1,
                request = new { id = (string?)null, idempotency_key = (string?)null },
                type,
                data = new { @object = dataObject }
            });

        private static string Session(string type, string sessionId, Guid orderId, string paymentStatus = "paid", string paymentIntent = "pi_test") =>
            Event(type, new
            {
                id = sessionId,
                @object = "checkout.session",
                payment_status = paymentStatus,
                payment_intent = paymentIntent,
                metadata = new Dictionary<string, string> { ["orderId"] = orderId.ToString() }
            });

        // How Stripe signs a webhook: HMAC-SHA256 of "{timestamp}.{body}" with the endpoint's secret
        private static string Sign(string json, string secret = TestDatabase.WebhookSecret)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{json}"));
            return $"t={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
        }

        private static string NewSessionId() => "cs_test_" + Guid.NewGuid().ToString("N");

        private ApplicationDbContext FreshDb(out IServiceScope scope)
        {
            scope = _db.NewScope();
            return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        [Fact]
        public async Task A_paid_session_fulfils_the_order_even_if_the_buyer_never_came_back()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var buyer = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            sp.GetRequiredService<IWishlistService>().Add(buyer.Id, game.Id);
            sp.GetRequiredService<ICartService>().Add(buyer.Id, game.Id);
            var sessionId = NewSessionId();
            var order = TestData.PendingOrder(sp, buyer.Id, sessionId, (game, buyer.Id));
            var json = Session("checkout.session.completed", sessionId, order.Id, paymentIntent: "pi_" + order.Id.ToString("N"));

            var result = sp.GetRequiredService<IOrderService>().HandleStripeWebhook(json, Sign(json));

            Assert.True(result.Succeeded);
            var db = FreshDb(out var check);
            using (check)
            {
                Assert.Equal(OrderStatus.Paid, db.Orders.Single(o => o.Id == order.Id).Status);
                Assert.Equal("pi_" + order.Id.ToString("N"), db.Orders.Single(o => o.Id == order.Id).PaymentIntentId);
                Assert.True(db.Libraries.Any(l => l.GamerId == buyer.Id && l.GameId == game.Id));
                Assert.False(db.Wishlists.Any(w => w.GamerId == buyer.Id));  // bought games leave the wishlist...
                Assert.False(db.CartItems.Any(c => c.UserId == buyer.Id));  // ...and the cart
            }
        }

        [Fact]
        public async Task Stripe_retrying_the_same_event_grants_and_notifies_once()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var orders = sp.GetRequiredService<IOrderService>();
            var buyer = await TestData.UserAsync(sp);
            var friend = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            var sessionId = NewSessionId();
            var order = TestData.PendingOrder(sp, buyer.Id, sessionId, (game, friend.Id)); // a gift
            var json = Session("checkout.session.completed", sessionId, order.Id);

            Assert.True(orders.HandleStripeWebhook(json, Sign(json)).Succeeded);
            Assert.True(orders.HandleStripeWebhook(json, Sign(json)).Succeeded);

            var db = FreshDb(out var check);
            using (check)
            {
                Assert.Equal(1, db.Libraries.Count(l => l.GamerId == friend.Id));
                Assert.Equal(1, db.Notifications.Count(n => n.UserId == friend.Id && n.Message.Contains("gift")));
            }
        }

        [Fact]
        public async Task A_paid_gift_for_a_deleted_account_doesnt_crash_the_webhook()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var buyer = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            var sessionId = NewSessionId();
            var order = TestData.PendingOrder(sp, buyer.Id, sessionId, (game, "account-deleted-meanwhile"), (TestData.Game(sp), buyer.Id));
            var json = Session("checkout.session.completed", sessionId, order.Id);

            // Must not throw: Stripe would retry a failing webhook forever
            Assert.True(sp.GetRequiredService<IOrderService>().HandleStripeWebhook(json, Sign(json)).Succeeded);

            var db = FreshDb(out var check);
            using (check)
            {
                Assert.Equal(OrderStatus.Paid, db.Orders.Single(o => o.Id == order.Id).Status);
                Assert.Equal(1, db.Libraries.Count(l => l.GamerId == buyer.Id)); // the buyer's own item still arrives
            }
        }

        [Fact]
        public async Task Refunding_an_order_of_a_deleted_account_doesnt_crash_the_webhook()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var buyer = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            var sessionId = NewSessionId();
            var paymentIntent = "pi_" + Guid.NewGuid().ToString("N");
            var order = TestData.PendingOrder(sp, buyer.Id, sessionId, (game, buyer.Id));
            var orders = sp.GetRequiredService<IOrderService>();
            var paid = Session("checkout.session.completed", sessionId, order.Id, paymentIntent: paymentIntent);
            orders.HandleStripeWebhook(paid, Sign(paid));
            Assert.True((await sp.GetRequiredService<IAccountService>().DeleteAccountAsync(buyer.Id, TestData.Password)).Succeeded);

            var refund = Event("charge.refunded", new { id = "ch_test", @object = "charge", payment_intent = paymentIntent, refunded = true });
            Assert.True(orders.HandleStripeWebhook(refund, Sign(refund)).Succeeded);

            var db = FreshDb(out var check);
            using (check) Assert.Equal(OrderStatus.Refunded, db.Orders.Single(o => o.Id == order.Id).Status);
        }

        [Fact]
        public async Task A_forged_request_is_rejected()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var orders = sp.GetRequiredService<IOrderService>();
            var attacker = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            var sessionId = NewSessionId();
            var order = TestData.PendingOrder(sp, attacker.Id, sessionId, (game, attacker.Id));
            var json = Session("checkout.session.completed", sessionId, order.Id);

            Assert.Equal(ResultStatus.Invalid, orders.HandleStripeWebhook(json, Sign(json, "whsec_guessed")).Status);
            Assert.Equal(ResultStatus.Invalid, orders.HandleStripeWebhook(json, null).Status);

            // A real signature, but for a different body
            var other = Session("checkout.session.completed", NewSessionId(), order.Id);
            Assert.Equal(ResultStatus.Invalid, orders.HandleStripeWebhook(json, Sign(other)).Status);

            var db = FreshDb(out var check);
            using (check) Assert.Equal(OrderStatus.Pending, db.Orders.Single(o => o.Id == order.Id).Status);
        }

        [Fact]
        public async Task A_session_must_belong_to_the_order_it_names()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var buyer = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            var order = TestData.PendingOrder(sp, buyer.Id, NewSessionId(), (game, buyer.Id));

            // Paid, but for some other checkout session: must not unlock this order
            var json = Session("checkout.session.completed", NewSessionId(), order.Id);
            Assert.True(sp.GetRequiredService<IOrderService>().HandleStripeWebhook(json, Sign(json)).Succeeded);

            var db = FreshDb(out var check);
            using (check) Assert.False(db.Libraries.Any(l => l.GamerId == buyer.Id));
        }

        [Fact]
        public async Task An_unpaid_session_grants_nothing_and_an_expired_one_cancels_the_order()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var orders = sp.GetRequiredService<IOrderService>();
            var buyer = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            var sessionId = NewSessionId();
            var order = TestData.PendingOrder(sp, buyer.Id, sessionId, (game, buyer.Id));

            var unpaid = Session("checkout.session.completed", sessionId, order.Id, paymentStatus: "unpaid");
            Assert.True(orders.HandleStripeWebhook(unpaid, Sign(unpaid)).Succeeded);
            var expired = Session("checkout.session.expired", sessionId, order.Id, paymentStatus: "unpaid");
            Assert.True(orders.HandleStripeWebhook(expired, Sign(expired)).Succeeded);

            var db = FreshDb(out var check);
            using (check)
            {
                Assert.False(db.Libraries.Any(l => l.GamerId == buyer.Id));
                Assert.Equal(OrderStatus.Cancelled, db.Orders.Single(o => o.Id == order.Id).Status);
            }
        }

        [Fact]
        public async Task A_full_refund_takes_the_games_back_and_tells_the_buyer()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var orders = sp.GetRequiredService<IOrderService>();
            var buyer = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            var sessionId = NewSessionId();
            var paymentIntent = "pi_" + Guid.NewGuid().ToString("N");
            var order = TestData.PendingOrder(sp, buyer.Id, sessionId, (game, buyer.Id));
            var paid = Session("checkout.session.completed", sessionId, order.Id, paymentIntent: paymentIntent);
            orders.HandleStripeWebhook(paid, Sign(paid));

            var refund = Event("charge.refunded", new { id = "ch_test", @object = "charge", payment_intent = paymentIntent, refunded = true });
            Assert.True(orders.HandleStripeWebhook(refund, Sign(refund)).Succeeded);

            var db = FreshDb(out var check);
            using (check)
            {
                Assert.Equal(OrderStatus.Refunded, db.Orders.Single(o => o.Id == order.Id).Status);
                Assert.False(db.Libraries.Any(l => l.GamerId == buyer.Id && l.GameId == game.Id));
                Assert.True(db.Notifications.Any(n => n.UserId == buyer.Id && n.Message.Contains("refunded")));
            }
        }
    }
}
