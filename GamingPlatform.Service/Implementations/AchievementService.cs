using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Service.Implementations
{
    public class AchievementService : IAchievementService
    {
        private readonly IRepository<Achievement> _achievementRepository;
        private readonly IGameService _gameService;

        public AchievementService(IRepository<Achievement> achievementRepository, IGameService gameService)
        {
            _achievementRepository = achievementRepository;
            _gameService = gameService;
        }

        public ServiceResult Add(Guid gameId, AchievementFormDto form, string userId, bool isAdmin)
        {
            var game = _gameService.GetGameById(gameId);
            if (game == null) return ServiceResult.NotFound();
            if (!_gameService.CanManage(game, userId, isAdmin)) return ServiceResult.Forbidden();

            _achievementRepository.Insert(new Achievement
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                Name = form.Name?.Trim(),
                Description = form.Description?.Trim()
            });
            return ServiceResult.Ok("Achievement added.");
        }

        public ServiceResult<Guid> Update(Guid id, AchievementFormDto form, string userId, bool isAdmin)
        {
            var achievement = _achievementRepository.Get(a => a, a => a.Id == id);
            if (achievement == null) return ServiceResult<Guid>.NotFound();
            if (!CanManage(achievement, userId, isAdmin)) return ServiceResult<Guid>.Forbidden();

            achievement.Name = form.Name?.Trim();
            achievement.Description = form.Description?.Trim();
            _achievementRepository.Update(achievement);
            return ServiceResult<Guid>.Success(achievement.GameId, "Achievement saved.");
        }

        public ServiceResult<Guid> Delete(Guid id, string userId, bool isAdmin)
        {
            var achievement = _achievementRepository.Get(a => a, a => a.Id == id);
            if (achievement == null) return ServiceResult<Guid>.NotFound();
            if (!CanManage(achievement, userId, isAdmin)) return ServiceResult<Guid>.Forbidden();

            _achievementRepository.Delete(achievement);
            return ServiceResult<Guid>.Success(achievement.GameId, "Achievement deleted.");
        }

        private bool CanManage(Achievement achievement, string userId, bool isAdmin)
        {
            var game = _gameService.GetGameById(achievement.GameId);
            return game != null && _gameService.CanManage(game, userId, isAdmin);
        }
    }
}
