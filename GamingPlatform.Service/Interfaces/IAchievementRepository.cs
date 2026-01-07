using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces;

public interface IAchievementRepository
{
    void AddAchievement(Guid gamerId, Achievement achievement);
    Achievement GetAchievementById(Guid id);
    List<Achievement> getAllAchievements();
    List<Achievement> getAllAchievementsBytGamer(Guid gamerId);
}