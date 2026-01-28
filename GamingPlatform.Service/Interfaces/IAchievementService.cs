using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces;

public interface IAchievementService
{
    void AddAchievement(string gamerid, Achievement achievement);
    Achievement GetAchievementById(Guid id);
    List<Achievement> getAllAchievements();
    List<Achievement> getAllAchievementsBytGamer(string gamerid);
}