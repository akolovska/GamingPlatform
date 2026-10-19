using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;
using Microsoft.AspNetCore.Http;

namespace GamingPlatform.Service.Interfaces
{
    public interface IGameService
    {
        // Store pages. viewerId (null when logged out) fills InLibrary/InWishlist/InCart.
        PagedResult<GameDto> Search(StoreQueryDto query, string? viewerId);
        List<GameDto> GetTrending(string? viewerId, int take = 8, int days = 7);
        List<RecommendationDto> GetRecommendations(string userId, int rows = 3, int perRow = 6);
        List<GameDto> GetByIds(IList<Guid> ids, string? viewerId, bool includeDelisted); // keeps the order of ids

        // null = missing, or delisted and not yours. reviewSort: "helpful" (default) or "newest".
        GameDetailsDto? GetDetails(Guid id, string? viewerId, bool isAdmin, string? reviewSort = null);

        Game? GetGameById(Guid id);

        // Admin: any game. Dev: only games of the studio linked to their account.
        bool CanManage(Game game, string? userId, bool isAdmin);

        ServiceResult<GameEditDto> GetForEdit(Guid id, string userId, bool isAdmin);
        Task<ServiceResult<Guid>> CreateAsync(GameFormDto form, IFormFile? cover, string userId, bool isAdmin);
        Task<ServiceResult> UpdateAsync(Guid id, GameFormDto form, IFormFile? cover, string userId, bool isAdmin);
        ServiceResult SetListed(Guid id, bool listed, string userId, bool isAdmin);

        // Screenshot gallery (up to 10 per game)
        Task<ServiceResult> AddScreenshotsAsync(Guid gameId, IList<IFormFile> files, string userId, bool isAdmin);
        ServiceResult DeleteScreenshot(Guid gameId, string url, string userId, bool isAdmin);
    }
}
