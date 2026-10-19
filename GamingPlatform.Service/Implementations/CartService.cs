using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GamingPlatform.Service.Implementations
{
    public class CartService : ICartService
    {
        private readonly IRepository<CartItem> _cartRepository;
        private readonly IRepository<Library> _libraryRepository;
        private readonly IGameService _gameService;

        public CartService(IRepository<CartItem> cartRepository, IRepository<Library> libraryRepository, IGameService gameService)
        {
            _cartRepository = cartRepository;
            _libraryRepository = libraryRepository;
            _gameService = gameService;
        }

        public CartDto GetCart(string userId)
        {
            var items = _cartRepository.GetAll(
                selector: c => c,
                predicate: c => c.UserId == userId,
                orderBy: q => q.OrderBy(c => c.AddedAt),
                include: q => q.Include(c => c.Game));

            // A game delisted (or bought some other way) since it was added can't be bought from the cart
            var owned = _libraryRepository.GetAll(l => l.GameId, l => l.GamerId == userId).ToHashSet();
            var stale = items.Where(c => c.Game.IsDelisted || owned.Contains(c.GameId)).ToList();
            if (stale.Count > 0) _cartRepository.DeleteRange(stale);

            var now = DateTime.UtcNow;
            return new CartDto
            {
                Items = items.Except(stale).Select(c =>
                {
                    bool onSale = Pricing.IsOnSale(c.Game, now) && (c.Game.Price ?? 0) > 0;
                    return new CartItemDto
                    {
                        GameId = c.GameId,
                        Title = c.Game.Title ?? "Untitled",
                        CoverImageUrl = c.Game.CoverImageUrl,
                        Price = Pricing.CurrentPrice(c.Game, now),
                        FullPrice = c.Game.Price,
                        DiscountPercent = onSale ? c.Game.DiscountPercent : 0,
                        IsComingSoon = c.Game.ReleaseDate > now
                    };
                }).ToList()
            };
        }

        public int Count(string userId) => _cartRepository.Count(c => c.UserId == userId);

        public ServiceResult Add(string userId, Guid gameId)
        {
            var game = _gameService.GetGameById(gameId);
            if (game == null || game.IsDelisted) return ServiceResult.NotFound();
            if (_libraryRepository.Any(l => l.GamerId == userId && l.GameId == gameId))
                return ServiceResult.Invalid("You already own this game.");

            // Adding twice is harmless: the unique (UserId, GameId) index keeps one row
            _cartRepository.TryInsert(new CartItem { Id = Guid.NewGuid(), UserId = userId, GameId = gameId, AddedAt = DateTime.UtcNow });
            return ServiceResult.Ok($"{game.Title} added to your cart.");
        }

        public ServiceResult Remove(string userId, Guid gameId)
        {
            var item = _cartRepository.Get(c => c, c => c.UserId == userId && c.GameId == gameId);
            if (item != null) _cartRepository.Delete(item);
            return ServiceResult.Ok("Removed from your cart.");
        }
    }
}
