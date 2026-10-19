using System.ComponentModel.DataAnnotations;

namespace GamingPlatform.Domain.DTO
{
    public class AchievementFormDto
    {
        [Required, StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
