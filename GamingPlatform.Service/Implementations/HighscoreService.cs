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
    public class HighScoreService : IHighScoreService
    {
        private readonly IRepository<HighScore> _highScoreRepository;
        public void AddHighScore(HighScore highScore)
        {
            _highScoreRepository.Insert(highScore);
        }

        public void UpdateHighScore(HighScore highScore)
        {
            _highScoreRepository.Update(highScore);
        }

        public void DeleteHighScore(Guid id)
        {
            HighScore highScore = getHighScoreById(id);
            _highScoreRepository.Delete(highScore);
        }

        public HighScore getHighScoreById(Guid id)
        {
            return _highScoreRepository.Get(selector:x => x,
                predicate: x => x.Id.Equals(id));
        }

        public List<HighScore> GetAllHighScores()
        {
            return _highScoreRepository.GetAll(selector: x => x).ToList();
        }
    }
}
