using GamingPlatform.Domain.DTO;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    [Collection("Database")]
    public class ReviewVoteTests
    {
        private readonly TestDatabase _db;
        public ReviewVoteTests(TestDatabase db) => _db = db;

        // A released game with one review by its owner; returns the review id
        private static async Task<(Guid GameId, Guid ReviewId, string AuthorId)> ReviewedGame(IServiceProvider sp, string comment = "review")
        {
            var author = await TestData.UserAsync(sp);
            var game = TestData.Game(sp);
            TestData.Own(sp, author.Id, game.Id);
            sp.GetRequiredService<IReviewService>().Add(author.Id, game.Id, new ReviewFormDto { Score = 4, Comment = comment });
            var reviewId = sp.GetRequiredService<ApplicationDbContext>().Reviews.Single(r => r.GamerId == author.Id).Id;
            return (game.Id, reviewId, author.Id);
        }

        [Fact]
        public async Task One_vote_per_person_which_can_be_switched_or_taken_back()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var reviews = sp.GetRequiredService<IReviewService>();
            var games = sp.GetRequiredService<IGameService>();
            var (gameId, reviewId, _) = await ReviewedGame(sp);
            var voter = await TestData.UserAsync(sp);

            ReviewDto Review() => games.GetDetails(gameId, voter.Id, false)!.Reviews.Single();

            reviews.Vote(reviewId, voter.Id, helpful: true);
            reviews.Vote(reviewId, voter.Id, helpful: true);  // same arrow again: takes it back
            Assert.Equal((0, 0, (bool?)null), (Review().Upvotes, Review().Downvotes, Review().MyVote));

            reviews.Vote(reviewId, voter.Id, helpful: true);
            reviews.Vote(reviewId, voter.Id, helpful: false); // switch
            Assert.Equal((0, 1, (bool?)false), (Review().Upvotes, Review().Downvotes, Review().MyVote));
        }

        [Fact]
        public async Task Nobody_can_vote_on_their_own_review()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var (gameId, reviewId, authorId) = await ReviewedGame(sp);

            Assert.Equal(ResultStatus.Invalid, sp.GetRequiredService<IReviewService>().Vote(reviewId, authorId, true).Status);
            Assert.False(sp.GetRequiredService<IGameService>().GetDetails(gameId, authorId, false)!.Reviews.Single().CanVote);
        }

        [Fact]
        public async Task Most_helpful_reviews_come_first()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var reviews = sp.GetRequiredService<IReviewService>();
            var (gameId, firstReview, _) = await ReviewedGame(sp, "older");
            var second = await TestData.UserAsync(sp);
            TestData.Own(sp, second.Id, gameId);
            reviews.Add(second.Id, gameId, new ReviewFormDto { Score = 2, Comment = "newer" });
            var secondReview = sp.GetRequiredService<ApplicationDbContext>().Reviews.Single(r => r.GamerId == second.Id).Id;

            for (int i = 0; i < 2; i++) reviews.Vote(firstReview, (await TestData.UserAsync(sp)).Id, helpful: true);

            var games = sp.GetRequiredService<IGameService>();
            Assert.Equal(firstReview, games.GetDetails(gameId, null, false, "helpful")!.Reviews[0].Id);
            Assert.Equal(secondReview, games.GetDetails(gameId, null, false, "newest")!.Reviews[0].Id);
        }

        [Fact]
        public async Task A_deleted_accounts_votes_go_with_it()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var (gameId, reviewId, _) = await ReviewedGame(sp);
            var voter = await TestData.UserAsync(sp);
            sp.GetRequiredService<IReviewService>().Vote(reviewId, voter.Id, true);

            Assert.True((await sp.GetRequiredService<IAccountService>().DeleteAccountAsync(voter.Id, TestData.Password)).Succeeded);

            Assert.Equal(0, sp.GetRequiredService<IGameService>().GetDetails(gameId, null, false)!.Reviews.Single().Upvotes);
        }
    }
}
