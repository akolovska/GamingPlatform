using GamingPlatform.Domain.DTO;

namespace GamingPlatform.Service.Interfaces
{
    // Only owners can review, once per game, after release. Only the author can edit a review;
    // the author or an Admin can delete it. Devs can't touch reviews of their games.
    public interface IReviewService
    {
        ServiceResult Add(string userId, Guid gameId, ReviewFormDto dto);

        // Value = the game id, so the caller can go back to the game
        ServiceResult<Guid> Update(Guid reviewId, string userId, ReviewFormDto dto);
        ServiceResult<Guid> Delete(Guid reviewId, string userId, bool isAdmin);

        // Up/down vote. Voting the same way again removes the vote. Not on your own review.
        ServiceResult<Guid> Vote(Guid reviewId, string userId, bool helpful);
    }
}
