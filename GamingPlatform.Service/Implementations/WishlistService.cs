using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Service.Implementations
{
    public class WishlistService : IWishlistService
    {
        private readonly IRepository<Wishlist> _wishlistRepository;
        private readonly IGameService _gameService;
        private readonly IGamerService _gamerService;
        public void AddToWishlist(Guid gamerId, Guid gameId)
        {
            Game game = _gameService.GetGameById(gameId);
            Gamer gamer = _gamerService.GetGamerById(gamerId);
            Wishlist wishlist = new Wishlist()
            {
                Game = game,
                GameId = gameId,
                Gamer = gamer,
                GamerId = gamerId,
                Id = Guid.NewGuid()
            };
            _wishlistRepository.Insert(wishlist);
        }

        public void DeleteWishlist(Guid gamerId, Guid gameId)
        {
            Wishlist wishlist = GetWishlist(gamerId, gameId);
            _wishlistRepository.Delete(wishlist);
        }

        public void UpdateGameInWishlist(Guid gamerId, Guid gameId)
        {
            Wishlist wishlist = GetWishlist(gamerId, gameId);
            _wishlistRepository.Update(wishlist);
        }

        public Wishlist GetWishlist(Guid gamerId, Guid gameId)
        {

            return _wishlistRepository.Get(selector: x => x,
                predicate: x => x.GameId.Equals(gameId) && x.GamerId.Equals(gamerId));
        }
        public List<Game> GetAllGamesFromWishlist(Guid gamerId)
        {
            return _wishlistRepository.GetAll(selector: x => x.Game,
                predicate: x => x.GamerId.Equals(gamerId)).ToList();
        }

    }
}
