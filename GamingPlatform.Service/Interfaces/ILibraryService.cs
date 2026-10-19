using GamingPlatform.Domain.DTO;

namespace GamingPlatform.Service.Interfaces
{
    // Owned games. Buying them is IOrderService's job.
    public interface ILibraryService
    {
        // ownerId's games; InWishlist/InLibrary are filled for viewerId
        List<GameDto> GetLibrary(string ownerId, string viewerId);
        GameDto? GetOwnedGame(string userId, Guid gameId);
    }
}
