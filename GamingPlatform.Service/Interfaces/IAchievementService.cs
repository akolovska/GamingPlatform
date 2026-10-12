using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces;

public interface IAchievementService
{
    void AddAchievement(Achievement achievement);
    Achievement GetAchievementById(Guid id);
    List<Achievement> getAllAchievements();
    List<Achievement> getAllAchievementsByGamer(string gamerid);
    void UpdateAchievement(Achievement achievement);
    void DeleteAchievement(Guid id);
}