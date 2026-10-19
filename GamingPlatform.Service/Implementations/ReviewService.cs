using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Service.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IRepository<ReviewVote> _voteRepository;
        private readonly IRepository<Game> _gameRepository;
        private readonly IRepository<Library> _libraryRepository;

        public ReviewService(
            IRepository<Review> reviewRepository,
            IRepository<ReviewVote> voteRepository,
            IRepository<Game> gameRepository,
            IRepository<Library> libraryRepository)
        {
            _reviewRepository = reviewRepository;
            _voteRepository = voteRepository;
            _gameRepository = gameRepository;
            _libraryRepository = libraryRepository;
        }

        public ServiceResult Add(string userId, Guid gameId, ReviewFormDto dto)
        {
            var releaseDate = _gameRepository.Get(g => new { g.ReleaseDate }, g => g.Id == gameId);
            if (releaseDate == null) return ServiceResult.NotFound();
            if (!_libraryRepository.Any(l => l.GamerId == userId && l.GameId == gameId)) return ServiceResult.Forbidden();

            // A pre-order can't be reviewed: nobody has played it yet
            if (releaseDate.ReleaseDate > DateTime.UtcNow)
                return ServiceResult.Invalid("You can review this game once it's released.");

            // The unique (GamerId, GameId) index allows one review per player per game
            var added = _reviewRepository.TryInsert(new Review
            {
                Id = Guid.NewGuid(),
                GamerId = userId,
                GameId = gameId,
                Score = dto.Score,
                Comment = dto.Comment?.Trim(),
                DateRated = DateTime.UtcNow
            });

            return added
                ? ServiceResult.Ok("Review posted.")
                : ServiceResult.Invalid("You have already reviewed this game.");
        }

        public ServiceResult<Guid> Update(Guid reviewId, string userId, ReviewFormDto dto)
        {
            var review = _reviewRepository.Get(r => r, r => r.Id == reviewId);
            if (review?.GameId == null) return ServiceResult<Guid>.NotFound();

            // Only the author: nobody else may change what a player said or scored
            if (review.GamerId != userId) return ServiceResult<Guid>.Forbidden();

            review.Score = dto.Score;
            review.Comment = dto.Comment?.Trim();
            _reviewRepository.Update(review);
            return ServiceResult<Guid>.Success(review.GameId.Value, "Review updated.");
        }

        public ServiceResult<Guid> Delete(Guid reviewId, string userId, bool isAdmin)
        {
            var review = _reviewRepository.Get(r => r, r => r.Id == reviewId);
            if (review?.GameId == null) return ServiceResult<Guid>.NotFound();

            // The author, or an Admin moderating. Not the game's Dev, who could hide bad reviews.
            if (review.GamerId != userId && !isAdmin) return ServiceResult<Guid>.Forbidden();

            _reviewRepository.Delete(review); // its votes go with it (cascade)
            return ServiceResult<Guid>.Success(review.GameId.Value, "Review deleted.");
        }

        public ServiceResult<Guid> Vote(Guid reviewId, string userId, bool helpful)
        {
            var review = _reviewRepository.Get(r => new { r.GamerId, r.GameId }, r => r.Id == reviewId);
            if (review?.GameId == null) return ServiceResult<Guid>.NotFound();

            // Voting on your own review would let anyone push it to the top
            if (review.GamerId == userId) return ServiceResult<Guid>.Invalid("You can't vote on your own review.");

            var existing = _voteRepository.Get(v => v, v => v.ReviewId == reviewId && v.UserId == userId);
            if (existing == null)
            {
                // The unique (ReviewId, UserId) index turns a double click into a no-op
                _voteRepository.TryInsert(new ReviewVote { Id = Guid.NewGuid(), ReviewId = reviewId, UserId = userId, IsHelpful = helpful });
            }
            else if (existing.IsHelpful == helpful)
            {
                _voteRepository.Delete(existing);    // clicking the same arrow again takes the vote back
            }
            else
            {
                existing.IsHelpful = helpful;         // switching from up to down or the other way
                _voteRepository.Update(existing);
            }
            return ServiceResult<Guid>.Success(review.GameId.Value);
        }
    }
}
