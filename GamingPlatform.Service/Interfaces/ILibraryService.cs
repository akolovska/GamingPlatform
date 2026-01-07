using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces
{
    public interface ILibraryService
    {
        void DeleteGameFromLibrary(Guid gamerId, Guid gameId);
        void AddGameToLibrary(Guid gamerId, Guid gameId);
        Library GetLibrary(Guid id);
        List<Game> GetAllGamesFromLibrary(Guid gamerId);
    }
}
