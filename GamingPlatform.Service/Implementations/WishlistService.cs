using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Service.Implementations
{
    public class WishlistService : IWishlistService
    {
        private readonly IRepository<Wishlist> _wishlistRepository;
        private readonly IGameService _gameService;

        public WishlistService(IRepository<Wishlist> wishlistRepository, IGameService gameService)
        {
            _wishlistRepository = wishlistRepository;
            _gameService = gameService;
        }

        public void AddGameToWishlist(string gamerid, Guid gameId)
        {
            Wishlist wishlist = GetWishlist(gamerid, gameId);
            if (wishlist != null)
            {
                throw new Exception("Game is already in the wishlist.");
            }
            var wishlistEntry = new Wishlist
            {
                GamerId = gamerid,
                GameId = gameId,
            };

            _wishlistRepository.Insert(wishlistEntry);
        }

        public void DeleteGameFromWishlist(string gamerid, Guid gameId)
        {
            Wishlist wishlist = GetWishlist(gamerid, gameId);
            if (wishlist == null)
            {
                throw new Exception("No such game.");
            }
            _wishlistRepository.Delete(wishlist);
        }

        public List<Game> GetAllGamesFromWishlist(string gamerid)
        {
            var gameIds = _wishlistRepository
                .GetAll(w => w, w => w.GamerId == gamerid) // Fix: use repository's predicate parameter
                .Select(w => w.GameId.Value) // GameId is nullable, so use .Value
                .ToList();

            // Fetch all games in one call
            return _gameService.GetGamesByIds(gameIds);
        }

        public Wishlist GetWishlist(string gamerid, Guid gameId)
        {
            return _wishlistRepository.Get(
                selector: w => w,
                predicate: w => w.GamerId == gamerid && w.GameId == gameId
            );

        }
        public bool IsGameInWishlist(string userId, Guid gameId)
        {
            return _wishlistRepository.GetAll(
                selector: x => x,
                predicate: l => l.GamerId == userId && l.GameId == gameId
            ).Any();
        }
    }
}
