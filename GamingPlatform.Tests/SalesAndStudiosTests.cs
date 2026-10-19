using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    [Collection("Database")]
    public class SalesAndStudiosTests
    {
        private readonly TestDatabase _db;
        public SalesAndStudiosTests(TestDatabase db) => _db = db;

        private int SaleNotifications(string userId, Guid gameId)
        {
            using var check = _db.NewScope();
            return check.ServiceProvider.GetRequiredService<ApplicationDbContext>().Notifications
                .Count(n => n.UserId == userId && n.Url == $"/Games/Details/{gameId}");
        }

        [Fact]
        public async Task A_scheduled_sale_is_announced_when_it_starts_by_notification_and_email_once()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var dev = await TestData.UserAsync(sp);
            var fan = await TestData.UserAsync(sp);
            var quietFan = await TestData.UserAsync(sp);
            var genre = TestData.Genre(sp);
            var game = TestData.Game(sp, price: 40, devUserId: dev.Id, genreId: genre.Id);
            var wishlist = sp.GetRequiredService<IWishlistService>();
            wishlist.Add(fan.Id, game.Id);
            wishlist.Add(quietFan.Id, game.Id);

            // quietFan turned sale emails off
            var db = sp.GetRequiredService<ApplicationDbContext>();
            db.Users.Single(u => u.Id == quietFan.Id).WishlistSaleEmails = false;
            db.SaveChanges();

            // The dev schedules a sale for next week: nothing is announced yet
            var games = sp.GetRequiredService<IGameService>();
            var form = games.GetForEdit(game.Id, dev.Id, false).Value!.Form;
            form.DiscountPercent = 25;
            form.DiscountStartsAt = DateTime.UtcNow.Date.AddDays(7);
            Assert.True((await games.UpdateAsync(game.Id, form, null, dev.Id, false)).Succeeded);
            Assert.Equal(0, SaleNotifications(fan.Id, game.Id));
            Assert.Equal(25, games.GetForEdit(game.Id, dev.Id, false).Value!.Form.DiscountPercent); // the form still shows it

            // A week later (simulated by moving the start back): the job announces it
            db.Games.Single(g => g.Id == game.Id).DiscountStartsAt = DateTime.UtcNow.AddHours(-1);
            db.SaveChanges();
            var emails = (FakeEmailSender)sp.GetRequiredService<IEmailSender>();
            var sentBefore = emails.Sent.Count;

            var announcer = sp.GetRequiredService<ISaleAnnouncer>();
            Assert.True(announcer.AnnounceStartedSales() >= 1);
            Assert.Equal(0, announcer.AnnounceStartedSales()); // the next run finds nothing new

            Assert.Equal(1, SaleNotifications(fan.Id, game.Id));
            Assert.Equal(1, SaleNotifications(quietFan.Id, game.Id));               // still gets the in-app notification...
            var newEmails = emails.Sent.Skip(sentBefore).ToList();
            Assert.Contains(newEmails, m => m.To == fan.Email && m.Body.Contains("-25%"));
            Assert.DoesNotContain(newEmails, m => m.To == quietFan.Email);          // ...but no email
        }

        [Fact]
        public async Task Studio_page_shows_listed_games_and_the_dashboard_counts_paid_sales_only()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var dev = await TestData.UserAsync(sp, Roles.Dev);
            var buyer = await TestData.UserAsync(sp);
            var game = TestData.Game(sp, price: 20, devUserId: dev.Id);
            var studioId = sp.GetRequiredService<IDevService>().GetByUserId(dev.Id)!.Id;

            // One paid order and one refunded order
            var paid = TestData.PendingOrder(sp, buyer.Id, "cs_paid_" + Guid.NewGuid().ToString("N"), (game, buyer.Id));
            var refunded = TestData.PendingOrder(sp, buyer.Id, "cs_refund_" + Guid.NewGuid().ToString("N"), (game, buyer.Id));
            var db = sp.GetRequiredService<ApplicationDbContext>();
            db.Orders.Single(o => o.Id == paid.Id).Status = OrderStatus.Paid;
            db.Orders.Single(o => o.Id == refunded.Id).Status = OrderStatus.Refunded;
            db.SaveChanges();
            TestData.Own(sp, buyer.Id, game.Id);

            var studios = sp.GetRequiredService<IStudioService>();
            var page = studios.GetPage(studioId, null, false)!;
            Assert.Equal(game.Id, page.Games.Single().Id);
            Assert.False(page.CanViewDashboard);
            Assert.True(studios.GetPage(studioId, dev.Id, false)!.CanViewDashboard);

            var dashboard = studios.GetDashboard(null, dev.Id, false).Value!;
            Assert.Equal(studioId, dashboard.StudioId);
            Assert.Equal(20m, dashboard.Revenue);    // the refunded order doesn't count
            Assert.Equal(1, dashboard.UnitsSold);
            Assert.Equal(1, dashboard.Owners);
            Assert.Equal(12, dashboard.RatingByMonth.Count);
        }

        [Fact]
        public async Task Devs_only_see_their_own_dashboard()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var dev = await TestData.UserAsync(sp, Roles.Dev);
            var otherDev = await TestData.UserAsync(sp, Roles.Dev);
            var devWithoutStudio = await TestData.UserAsync(sp, Roles.Dev);
            TestData.Game(sp, devUserId: dev.Id);
            TestData.Game(sp, devUserId: otherDev.Id);
            var otherStudio = sp.GetRequiredService<IDevService>().GetByUserId(otherDev.Id)!.Id;
            var studios = sp.GetRequiredService<IStudioService>();

            Assert.Equal(ResultStatus.Forbidden, studios.GetDashboard(otherStudio, dev.Id, false).Status);
            Assert.Equal(ResultStatus.Invalid, studios.GetDashboard(null, devWithoutStudio.Id, false).Status);
            Assert.True(studios.GetDashboard(otherStudio, dev.Id, isAdmin: true).Succeeded); // admins see any studio
        }
    }
}
