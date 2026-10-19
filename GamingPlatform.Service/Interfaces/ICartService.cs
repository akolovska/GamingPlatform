using GamingPlatform.Domain.DTO;

namespace GamingPlatform.Service.Interfaces
{
    // The cart holds games to buy for yourself (gifts are bought one at a time from the game page)
    public interface ICartService
    {
        CartDto GetCart(string userId);       // current prices; games that can't be bought any more are dropped
        int Count(string userId);
        ServiceResult Add(string userId, Guid gameId);
        ServiceResult Remove(string userId, Guid gameId);
    }
}
