using GamingPlatform.Domain.DTO;

namespace GamingPlatform.Service.Interfaces
{
    public interface IWishlistService
    {
        // Delisted games are hidden from wishlists (they come back if the game is relisted)
        List<GameDto> GetMyWishlist(string userId);
        ServiceResult<WishlistDto> GetWishlist(string ownerId, string viewerId);

        ServiceResult Add(string userId, Guid gameId);
        ServiceResult Remove(string userId, Guid gameId);
    }
}
