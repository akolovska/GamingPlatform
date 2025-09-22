using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces
{
    public interface IHighScoreService
    {
        void AddHighScore(HighScore highScore);
        void UpdateHighScore(HighScore highScore);
        void DeleteHighScore(Guid id);
        HighScore getHighScoreById(Guid id);
        List<HighScore> GetAllHighScores();
    }
}
