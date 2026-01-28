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
    public class GameService : IGameService
    {
        private readonly IRepository<Game> _gameRepository;

        public GameService(IRepository<Game> gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public void AddGame(Game game)
        {
            _gameRepository.Insert(game);
        }

        public void DeleteGame(Guid id)
        {
            Game game = GetGameById(id);
            _gameRepository.Delete(game);
        }

        public List<Game> GetAllGames()
        {
            return _gameRepository.GetAll(selector: x => x).ToList();
        }

        public Game GetGameById(Guid id)
        {
            return _gameRepository.Get(selector: x => x,
                predicate: x => x.Id.Equals(id));
        }

        public void UpdateGame(Game game)
        {
            _gameRepository.Update(game);
        }
        public List<Game> GetGamesByIds(List<Guid> ids)
        {
            return _gameRepository.GetAll(
                selector: x => x,
                predicate: x => ids.Contains(x.Id)
            ).ToList();
        }

    }
}
