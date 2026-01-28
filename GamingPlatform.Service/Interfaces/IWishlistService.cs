using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces
{
    public interface IWishlistService
    {
        void AddGameToWishlist(string gamerid, Guid gameId);
        void DeleteGameFromWishlist(string gamerid, Guid gameId);
        Wishlist GetWishlist(string gamerid, Guid gameId);
        List<Game> GetAllGamesFromWishlist(string gamerid);
        bool IsGameInWishlist(string userId, Guid gameId);
    }
}
