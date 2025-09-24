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
        void AddLibrary(Library library);
        // void AddLibrary(Guid gamerId, Guid gameId);
        void DeleteLibrary(Guid gamerId, Guid gameId);
        void UpdateLibrary(Guid gamerId, Guid gameId);
        // Library GetLibrary (Guid gamerId, Guid gameId);
        Library GetLibraryById(Guid id);
        List<Game> GetAllGamesFromLibrary(Guid gamerId);
        List<Library> GetAllLibrariesByUser(Guid gamerId);
    }
}
