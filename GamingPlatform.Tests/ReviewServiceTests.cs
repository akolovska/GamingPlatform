using GamingPlatform.Domain.DTO;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    [Collection("Database")]
    public class ReviewServiceTests
    {
        private readonly TestDatabase _db;
        public ReviewServiceTests(TestDatabase db) => _db = db;

        private static ReviewFormDto Form(int score, string comment = "comment") => new() { Score = score, Comment = comment };

        [Fact]
        public async Task Players_can_only_review_games_they_own()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var player = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);

            var result = sp.GetRequiredService<IReviewService>().Add(player.Id, game.Id, Form(5));

            Assert.Equal(ResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task An_owner_can_review_once()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var reviews = sp.GetRequiredService<IReviewService>();
            var player = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            TestData.Own(sp, player.Id, game.Id);

            Assert.True(reviews.Add(player.Id, game.Id, Form(4)).Succeeded);
            var second = reviews.Add(player.Id, game.Id, Form(1));

            Assert.Equal(ResultStatus.Invalid, second.Status);
            Assert.Equal(1, sp.GetRequiredService<ApplicationDbContext>().Reviews.Count(r => r.GamerId == player.Id));
        }

        [Fact]
        public async Task Nobody_but_the_author_can_edit_a_review_not_even_the_games_dev()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var reviews = sp.GetRequiredService<IReviewService>();
            var dev = await TestData.UserAsync(sp);
            var player = await TestData.UserAsync(sp);
            var game = TestData.Game(sp, devUserId: dev.Id);
            TestData.Own(sp, player.Id, game.Id);
            reviews.Add(player.Id, game.Id, Form(1, "bad game"));
            var reviewId = sp.GetRequiredService<ApplicationDbContext>().Reviews.Single(r => r.GamerId == player.Id).Id;

            Assert.Equal(ResultStatus.Forbidden, reviews.Update(reviewId, dev.Id, Form(5, "great game")).Status);
            Assert.Equal(ResultStatus.Forbidden, reviews.Delete(reviewId, dev.Id, isAdmin: false).Status);
            Assert.True(reviews.Update(reviewId, player.Id, Form(2, "meh")).Succeeded);
        }

        [Fact]
        public async Task An_admin_can_delete_any_review()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var reviews = sp.GetRequiredService<IReviewService>();
            var admin = await TestData.UserAsync(sp);
            var player = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            TestData.Own(sp, player.Id, game.Id);
            reviews.Add(player.Id, game.Id, Form(1, "spam"));
            var reviewId = sp.GetRequiredService<ApplicationDbContext>().Reviews.Single(r => r.GamerId == player.Id).Id;

            var result = reviews.Delete(reviewId, admin.Id, isAdmin: true);

            Assert.True(result.Succeeded);
            Assert.Equal(game.Id, result.Value);
        }
    }
}
