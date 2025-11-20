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
        // public void AddLibrary(Guid gamerId, Guid gameId)
        // {
        //     Game game = _gameService.GetGameById(gameId);
        //     Gamer gamer = _gamerService.GetGamerById(gamerId);
        //     Library library = new Library()
        //     {
        //         DateAdded = DateTime.Now,
        //         Game = game,
        //         GameId = gameId,
        //         GamerId = gamerId,
        //         Gamer = gamer,
        //         Id = Guid.NewGuid(),
        //         PlayTimeHours = 0
        //     };
        //     _libraryRepository.Insert(library);
        // }

        public void AddLibrary(Library library)
        {
            _libraryRepository.Insert(library);
        }

        public void DeleteLibrary(Guid libraryId)
        {
            Library library = GetLibraryById(libraryId);
            _libraryRepository.Delete(library);
        }

        public void UpdateLibrary(Library library)
        {
            _libraryRepository.Update(library);
        }


        public Library GetLibrary(Guid gamerId, Guid gameId)
        {
            return _libraryRepository.Get(selector: x => x,
                predicate: x => x.GamerId.Equals(gamerId) && x.GameId.Equals(gameId));
        }
        public Library GetLibraryById(Guid id)
        {
            return _libraryRepository.Get(selector: x => x,
                predicate: x => x.Id.Equals(id));
        }

        public List<Game> GetAllGamesFromLibrary(Guid gamerId)
        {
            return _libraryRepository.GetAll(selector: x => x.Game,
                predicate: x => x.GamerId.Equals(gamerId)).ToList();
        }

        public List<Library> GetAllLibrariesByUser(Guid gamerId)
        {
            return _libraryRepository.GetAll(selector: x => x,
                predicate: x => x.GamerId.Equals(gamerId)).ToList();
        }
    }
}
