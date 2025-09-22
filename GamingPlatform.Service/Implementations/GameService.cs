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
    public class GameService : IGameService
    {
        private readonly IRepository<Game> _gameRepository;
        public void AddGame(Game game)
        {
            _gameRepository.Insert(game);
        }

        public void DeleteGame(Guid id)
        {
            Game game = GetGameById(id);
            _gameRepository.Delete(game);
        }

        public void UpdateGame(Game game)
        {
            _gameRepository.Update(game);
        }

        public Game GetGameById(Guid id)
        {
            return _gameRepository.Get(selector: x => x,
                predicate: x => x.Id.Equals(id));
        }

        public List<Game> GetAllGames()
        {
            return _gameRepository.GetAll(selector: x => x).ToList();
        }
    }
}
