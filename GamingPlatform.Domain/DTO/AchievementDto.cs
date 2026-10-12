using GamingPlatform.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.DTO
{
    public class AchievementDto
    {
        public Guid Id { get; set; }
        public Guid GameId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsUnlocked { get; set; }
        public DateTime? DateUnlocked { get; set; }
        public static Achievement ToAchievement(AchievementDto dto)
        {
            return new Achievement
            {
                Id = dto.Id,
                GameId = dto.GameId,
                Name = dto.Name,
                Description = dto.Description
            };
        }
        public static AchievementDto FromAchievement(Achievement achievement)
        {
            return new AchievementDto
            {
                Id = achievement.Id,
                GameId = achievement.GameId,
                Name = achievement.Name,
                Description = achievement.Description,
                IsUnlocked = false,
                DateUnlocked = null
            };
        }
    }
}
