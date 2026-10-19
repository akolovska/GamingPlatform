using GamingPlatform.Domain.DTO;

namespace GamingPlatform.Service.Interfaces;

// Achievements are managed by whoever can manage the game (its studio's Dev, or an Admin)
public interface IAchievementService
{
    ServiceResult Add(Guid gameId, AchievementFormDto form, string userId, bool isAdmin);

    // Value = the game id, so the caller can go back to the game
    ServiceResult<Guid> Update(Guid id, AchievementFormDto form, string userId, bool isAdmin);
    ServiceResult<Guid> Delete(Guid id, string userId, bool isAdmin);
}
