using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Service.Implementations
{
    public class LibraryService : ILibraryService
    {
        private readonly IRepository<Library> _libraryRepository;
        private readonly IGameService _gameService;
        public LibraryService(IRepository<Library> libraryRepository, IGameService gameService)
        {
            _libraryRepository = libraryRepository;
            _gameService = gameService;
        }

        // Everything the library pages show about a game
        private static IIncludableQueryable<Library, object> IncludeGameDetails(IQueryable<Library> q) =>
            q.Include(l => l.Game!).ThenInclude(g => g.GameGenres).ThenInclude(gg => gg.Genre)
             .Include(l => l.Game!).ThenInclude(g => g.GameDevelopers).ThenInclude(gd => gd.Developer)
             .Include(l => l.Game!).ThenInclude(g => g.Achievements)
             .Include(l => l.Game!).ThenInclude(g => g.Reviews);

        public void AddGameToLibrary(string gamerid, Guid gameId)
        {
            _libraryRepository.Insert(new Library
            {
                Id = Guid.NewGuid(),
                GamerId = gamerid,
                GameId = gameId,
                DateAdded = DateTime.UtcNow,
                PlayTimeHours = 0
            });
        }

        public void DeleteGameFromLibrary(string gamerid, Guid gameId)
        {
            Library entry = _libraryRepository.Get(
                selector: l => l,
                predicate: l => l.GamerId == gamerid && l.GameId == gameId
            );
            _libraryRepository.Delete(entry);
        }

        public List<Library> GetAllLibraries()
        {
            return _libraryRepository.GetAll(x => x).ToList();
        }

        public Library GetLibrary(string gamerid)
        {
            return _libraryRepository.Get(selector: x => x, predicate: l => l.GamerId == gamerid);
        }

        // Selects the Library row (not l.Game) so EF applies the includes; they're ignored when the projection changes type
        public List<Game> GetUserGames(string gamerid)
        {
            return _libraryRepository.GetAll(
                selector: l => l,
                predicate: l => l.GamerId == gamerid,
                include: IncludeGameDetails
            ).Select(l => l.Game!).ToList();
        }
        public bool IsGameInLibrary(string userId, Guid gameId)
        {
            return _libraryRepository.GetAll(
                selector: x => x,
                predicate: l => l.GamerId == userId && l.GameId == gameId
            ).Any();
        }
        public Game GetGameFromLibrary(string userId, Guid gameId)
        {
            var entry = _libraryRepository.Get(
                selector: l => l,
                predicate: l => l.GamerId == userId && l.GameId == gameId,
                include: IncludeGameDetails
            );
            return entry?.Game;
        }

    }
}
