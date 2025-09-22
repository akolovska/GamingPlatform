using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces
{
    public interface IGameService
    {
        void AddGame(Game game);
        void DeleteGame(Guid id);
        void UpdateGame(Game game);
        Game GetGameById(Guid id);
        List<Game> GetAllGames();
    }
}
