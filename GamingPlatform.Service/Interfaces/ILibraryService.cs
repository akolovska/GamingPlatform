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
        void AddToLibrary(Guid gamerId, Guid gameId);
        void DeleteFromLibrary(Guid gamerId, Guid gameId);
        void UpdateGameInLibrary(Guid gamerId, Guid gameId);
        Library GetLibrary (Guid gamerId, Guid gameId);
        List<Game> GetAllGamesFromLibrary(Guid gamerId);
    }
}
