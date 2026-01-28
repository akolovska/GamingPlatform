using GamingPlatform.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Service.Interfaces
{
    public interface ILibraryService
    {
        void DeleteGameFromLibrary(string gamerid, Guid gameId);
        void AddGameToLibrary(string gamerid, Guid gameId);
        Library GetLibrary(string gamerid);
        List<Game> GetUserGames(string gamerid);
        List<Library> GetAllLibraries();
        bool IsGameInLibrary(string userId, Guid gameId);
        Game GetGameFromLibrary(string userId, Guid gameId);

    }
}
