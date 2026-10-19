using GamingPlatform.Domain.API;

namespace GamingPlatform.Service.Interfaces
{
    // Reads the public FreeToGame catalog. Returns null when the API is unavailable.
    public interface IFreeToGameService
    {
        Task<List<FreeToGameGame>?> GetGamesAsync();
        Task<FreeToGameGame?> GetGameAsync(int id);
    }
}
