using GamingPlatform.Domain.DTO;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    [Collection("Database")]
    public class StoreTests
    {
        private readonly TestDatabase _db;
        public StoreTests(TestDatabase db) => _db = db;

        [Fact]
        public void Search_filters_in_the_database()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var games = sp.GetRequiredService<IGameService>();
            var tag = Guid.NewGuid().ToString("N")[..6];   // a word only this test's games have
            var free = TestData.Game(sp, price: 0, title: $"Free {tag}");
            var sale = TestData.Game(sp, price: 30, discountPercent: 50, title: $"Sale {tag}");
            var soon = TestData.Game(sp, price: 10, releaseDate: DateTime.UtcNow.AddMonths(2), title: $"Soon {tag}");
            TestData.Game(sp, price: 10, delisted: true, title: $"Hidden {tag}");

            List<Guid> Ids(StoreQueryDto q) => games.Search(q, null).Items.Select(g => g.Id).ToList();

            Assert.Equal(3, Ids(new StoreQueryDto { Q = tag }).Count);                       // delisted never shows
            Assert.Equal(new[] { free.Id }, Ids(new StoreQueryDto { Q = tag, Free = true }));
            Assert.Equal(new[] { sale.Id }, Ids(new StoreQueryDto { Q = tag, OnSale = true }));
            Assert.Equal(new[] { soon.Id }, Ids(new StoreQueryDto { Q = tag, ComingSoon = true }));
            // Sorting by price uses today's price: free 0, soon 10, sale 15 (30 at -50%)
            Assert.Equal(new[] { free.Id, soon.Id, sale.Id }, Ids(new StoreQueryDto { Q = tag, Sort = StoreSort.PriceLow }));
        }

        [Fact]
        public void Results_come_in_pages()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var tag = Guid.NewGuid().ToString("N")[..6];
            for (int i = 0; i < 26; i++) TestData.Game(sp, title: $"Paged {tag} {i:00}");

            var page2 = sp.GetRequiredService<IGameService>().Search(new StoreQueryDto { Q = tag, Page = 2 }, null);

            Assert.Equal(26, page2.TotalCount);
            Assert.Equal(2, page2.TotalPages);
            Assert.Equal(2, page2.Items.Count);                    // 24 on page 1, 2 on page 2
            Assert.Equal($"Paged {tag} 24", page2.Items[0].Title); // in name order
        }

        [Fact]
        public async Task Because_you_played_suggests_similar_games_you_dont_own()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var player = await TestData.UserAsync(sp);
            var strategy = TestData.Genre(sp);
            var puzzle = TestData.Genre(sp);
            var played = TestData.Game(sp, genreId: strategy.Id, title: "Played " + Guid.NewGuid());
            var similar = TestData.Game(sp, genreId: strategy.Id);
            var alsoOwned = TestData.Game(sp, genreId: strategy.Id);
            var delisted = TestData.Game(sp, genreId: strategy.Id, delisted: true);
            var unrelated = TestData.Game(sp, genreId: puzzle.Id);
            TestData.Own(sp, player.Id, played.Id);
            TestData.Own(sp, player.Id, alsoOwned.Id);

            var rows = sp.GetRequiredService<IGameService>().GetRecommendations(player.Id, rows: 3, perRow: 50);
            var suggested = rows.SelectMany(r => r.Games).Select(g => g.Id).ToList();

            Assert.Contains(similar.Id, suggested);
            Assert.DoesNotContain(alsoOwned.Id, suggested);
            Assert.DoesNotContain(played.Id, suggested);
            Assert.DoesNotContain(delisted.Id, suggested);
            Assert.DoesNotContain(unrelated.Id, suggested);
            Assert.Equal(suggested.Count, suggested.Distinct().Count()); // never the same game twice
        }

        [Fact]
        public async Task Pre_orders_cant_be_reviewed_before_release()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var player = await TestData.UserAsync(sp);
            var game = TestData.Game(sp, releaseDate: DateTime.UtcNow.AddDays(30));
            TestData.Own(sp, player.Id, game.Id);

            var result = sp.GetRequiredService<IReviewService>().Add(player.Id, game.Id, new ReviewFormDto { Score = 5, Comment = "hype" });

            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.False(sp.GetRequiredService<IGameService>().GetDetails(game.Id, player.Id, false)!.CanReview);
        }

        [Fact]
        public async Task Starting_a_sale_notifies_everyone_who_wishlisted_the_game()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var dev = await TestData.UserAsync(sp);
            var fan = await TestData.UserAsync(sp);
            var genre = TestData.Genre(sp);
            var game = TestData.Game(sp, price: 20, devUserId: dev.Id, genreId: genre.Id);
            sp.GetRequiredService<IWishlistService>().Add(fan.Id, game.Id);

            var games = sp.GetRequiredService<IGameService>();
            var form = games.GetForEdit(game.Id, dev.Id, false).Value!.Form;
            form.DiscountPercent = 40;
            Assert.True((await games.UpdateAsync(game.Id, form, null, dev.Id, false)).Succeeded);
            Assert.True((await games.UpdateAsync(game.Id, form, null, dev.Id, false)).Succeeded); // saving again doesn't re-notify

            using var check = _db.NewScope();
            var db = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, db.Notifications.Count(n => n.UserId == fan.Id && n.Message.Contains("-40%")));
        }

        [Fact]
        public async Task Screenshots_are_added_and_removed()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var games = sp.GetRequiredService<IGameService>();
            var dev = await TestData.UserAsync(sp);
            var game = TestData.Game(sp, devUserId: dev.Id);
            var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
            var file = new Microsoft.AspNetCore.Http.FormFile(new MemoryStream(png), 0, png.Length, "screenshots", "shot.png");

            Assert.True((await games.AddScreenshotsAsync(game.Id, new[] { file }, dev.Id, false)).Succeeded);
            string url;
            using (var check = _db.NewScope())
                url = check.ServiceProvider.GetRequiredService<ApplicationDbContext>().Games.Single(g => g.Id == game.Id).Screenshots.Single();

            using (var other = _db.NewScope()) // a new request, like the Delete button
            {
                var result = other.ServiceProvider.GetRequiredService<IGameService>().DeleteScreenshot(game.Id, url, dev.Id, false);
                Assert.True(result.Succeeded, result.Message);
            }
            using (var check = _db.NewScope())
                Assert.Empty(check.ServiceProvider.GetRequiredService<ApplicationDbContext>().Games.Single(g => g.Id == game.Id).Screenshots);
            Assert.False(File.Exists(Path.Combine(_db.WebRoot, url.TrimStart('/'))));
        }

        [Fact]
        public async Task Screenshots_can_only_be_removed_by_their_own_url()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var dev = await TestData.UserAsync(sp);
            var game = TestData.Game(sp, devUserId: dev.Id);

            // Not one of this game's screenshots: refused, so the posted value can't point at other files
            var result = sp.GetRequiredService<IGameService>().DeleteScreenshot(game.Id, "/uploads/avatars/someone-else.png", dev.Id, false);

            Assert.Equal(ResultStatus.NotFound, result.Status);
        }
    }
}
