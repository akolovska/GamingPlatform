using System;
using System.Collections.Generic;
using System.Linq;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Service.Implementations
{
    public class AchievementService : IAchievementService
    {
        private readonly IRepository<Achievement> _achievementRepository;
        private readonly IRepository<GamerAchievement> _gamerAchievementRepository;

        public AchievementService(IRepository<Achievement> achievementRepository, IRepository<GamerAchievement> gamerAchievementRepository)
        {
            _achievementRepository = achievementRepository;
            _gamerAchievementRepository = gamerAchievementRepository;
        }

        public void AddAchievement(Achievement achievement)
        {
            if (achievement == null) return;
            if (achievement.Id == default)
            {
                achievement.Id = Guid.NewGuid();
            }
            _achievementRepository.Insert(achievement);
        }

        public void DeleteAchievement(Guid id)
        {
            if (id == null) return;
            var achievement = _achievementRepository.Get(x => x, predicate: x => x.Id == id);
            if (achievement != null)
            {
                _achievementRepository.Delete(achievement);
            }
        }

        public void UpdateAchievement(Achievement achievement)
        {
            if (achievement == null) return;
            if (achievement.Id == default)
            {
                achievement.Id = Guid.NewGuid();
                _achievementRepository.Insert(achievement);
            }
            else
                _achievementRepository.Update(achievement);
        }

        public Achievement GetAchievementById(Guid id) => _achievementRepository.Get(x => x, predicate: x => x.Id == id);

        public List<Achievement> getAllAchievements()
        {
            return _achievementRepository.GetAll(x => x).ToList();
        }

        public List<Achievement> getAllAchievementsByGamer(string gamerid)
        {
            var gaIds = _gamerAchievementRepository.GetAll(x => x, predicate: x => x.GamerId == gamerid)
                .Select(x => x.AchievementId)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();

            if (!gaIds.Any()) return new List<Achievement>();

            return _achievementRepository.GetAll(x => x, predicate: x => gaIds.Contains(x.Id)).ToList();
        }
    }
}
