using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        public List<Game> GetUserGames(string gamerid)
        {
            return _libraryRepository.GetAll(selector: l => l.Game!, predicate: l => l.GamerId == gamerid).ToList();
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
            return _libraryRepository.Get(
                selector: l => l.Game!,
                predicate: l => l.GamerId == userId && l.GameId == gameId
            );
        }

    }
}
