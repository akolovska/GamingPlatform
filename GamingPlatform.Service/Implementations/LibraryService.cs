using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Service.Implementations
{
    public class LibraryService : ILibraryService
    {
        private readonly IRepository<Library> _libraryRepository;
        private readonly IGameService _gameService;
        private readonly IGamerService _gamerService;
        public void AddToLibrary(Guid gamerId, Guid gameId)
        {
            Game game = _gameService.GetGameById(gameId);
            Gamer gamer = _gamerService.GetGamerById(gamerId);
            Library library = new Library()
            {
                DateAdded = DateTime.Now,
                Game = game,
                GameId = gameId,
                GamerId = gamerId,
                Gamer = gamer,
                Id = Guid.NewGuid(),
                PlayTimeHours = 0
            };
            _libraryRepository.Insert(library);
        }

        public void DeleteFromLibrary(Guid gamerId, Guid gameId)
        {
            Library library = GetLibrary(gamerId, gameId);
            _libraryRepository.Delete(library);
        }

        public void UpdateGameInLibrary(Guid gamerId, Guid gameId)
        {
            Library library = GetLibrary(gamerId, gameId);
            _libraryRepository.Update(library);
        }

        public Library GetLibrary(Guid gamerId, Guid gameId)
        {
            return _libraryRepository.Get(selector: x => x,
                predicate: x => x.GamerId.Equals(gamerId) && x.GameId.Equals(gameId));
        }

        public List<Game> GetAllGamesFromLibrary(Guid gamerId)
        {
            return _libraryRepository.GetAll(selector: x => x.Game,
                predicate: x => x.GamerId.Equals(gamerId)).ToList();
        }
    }
}
