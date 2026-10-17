using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces
{
    public interface IReviewService
    {
        Review? GetReviewById(Guid id);
        Review? GetUserReviewForGame(string gamerId, Guid gameId);
        void AddReview(string gamerId, Guid gameId, ReviewFormDto dto);
        void UpdateReview(Review review, ReviewFormDto dto);
        void DeleteReview(Review review);
    }
}
