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
        void AddToWishlist(Guid gamerId, Guid gameId);
        void DeleteWishlist(Guid gamerId, Guid gameId);
        void UpdateGameInWishlist(Guid gamerId, Guid gameId);
        Wishlist GetWishlist(Guid gamerId, Guid gameId);
        List<Game> GetAllGamesFromWishlist(Guid gamerId);
    }
}
