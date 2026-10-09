using GamingPlatform.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Service.Interfaces
{
    public interface IGameService
    {
        void AddGame(Game game);
        void DeleteGame(Guid id);
        void UpdateGame(Game game);
        Game? GetGameById(Guid id);
        List<Game> GetAllGames(bool includeDelisted = false);
        List<Game> GetGamesByIds(List<Guid> ids);
        List<Game> GetTrending(int take = 8, int days = 7);
    }
}
