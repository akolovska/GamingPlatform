using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace GamingPlatform.Service.Implementations
{
    public class WishlistService : IWishlistService
    {
        private readonly IRepository<Wishlist> _wishlistRepository;
        private readonly IRepository<Library> _libraryRepository;
        private readonly IGameService _gameService;
        private readonly IFriendService _friendService;
        private readonly UserManager<GamingPlatformUser> _userManager;

        public WishlistService(
            IRepository<Wishlist> wishlistRepository,
            IRepository<Library> libraryRepository,
            IGameService gameService,
            IFriendService friendService,
            UserManager<GamingPlatformUser> userManager)
        {
            _wishlistRepository = wishlistRepository;
            _libraryRepository = libraryRepository;
            _gameService = gameService;
            _friendService = friendService;
            _userManager = userManager;
        }

        public List<GameDto> GetMyWishlist(string userId) =>
            _gameService.GetByIds(GameIds(userId), userId, includeDelisted: false);

        public ServiceResult<WishlistDto> GetWishlist(string ownerId, string viewerId)
        {
            var owner = _userManager.Users.FirstOrDefault(u => u.Id == ownerId);
            // A block hides the user completely, like an account that doesn't exist
            if (owner == null || _friendService.IsBlockedEitherWay(ownerId, viewerId))
                return ServiceResult<WishlistDto>.NotFound();

            var dto = new WishlistDto
            {
                OwnerName = owner.UserName ?? "Unknown",
                IsVisible = _friendService.CanViewWishlist(owner, viewerId)
            };
            if (dto.IsVisible)
                dto.Games = _gameService.GetByIds(GameIds(ownerId), viewerId, includeDelisted: false);
            return ServiceResult<WishlistDto>.Success(dto);
        }

        public ServiceResult Add(string userId, Guid gameId)
        {
            var game = _gameService.GetGameById(gameId);
            if (game == null || game.IsDelisted) return ServiceResult.NotFound();

            if (_libraryRepository.Any(l => l.GamerId == userId && l.GameId == gameId))
                return ServiceResult.Invalid("You already own this game.");

            // Adding twice is harmless: the unique (GamerId, GameId) index keeps one row
            _wishlistRepository.TryInsert(new Wishlist { GamerId = userId, GameId = gameId });
            return ServiceResult.Ok("Added to your wishlist.");
        }

        public ServiceResult Remove(string userId, Guid gameId)
        {
            var entry = _wishlistRepository.Get(w => w, w => w.GamerId == userId && w.GameId == gameId);
            if (entry != null) _wishlistRepository.Delete(entry);
            return ServiceResult.Ok("Removed from your wishlist.");
        }

        private List<Guid> GameIds(string userId) =>
            _wishlistRepository.GetAll(w => w.GameId!.Value, w => w.GamerId == userId && w.GameId != null);
    }
}
