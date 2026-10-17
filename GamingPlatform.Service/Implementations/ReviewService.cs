using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Service.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly IRepository<Review> _reviewRepository;

        public ReviewService(IRepository<Review> reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        public Review? GetReviewById(Guid id) =>
            _reviewRepository.Get(x => x, predicate: x => x.Id == id);

        public Review? GetUserReviewForGame(string gamerId, Guid gameId) =>
            _reviewRepository.Get(x => x, predicate: x => x.GamerId == gamerId && x.GameId == gameId);

        public void AddReview(string gamerId, Guid gameId, ReviewFormDto dto)
        {
            _reviewRepository.Insert(new Review
            {
                Id = Guid.NewGuid(),
                GamerId = gamerId,
                GameId = gameId,
                Score = dto.Score,
                Comment = dto.Comment,
                DateRated = DateTime.UtcNow
            });
        }

        public void UpdateReview(Review review, ReviewFormDto dto)
        {
            review.Score = dto.Score;
            review.Comment = dto.Comment;
            _reviewRepository.Update(review);
        }

        public void DeleteReview(Review review) => _reviewRepository.Delete(review);
    }
}
