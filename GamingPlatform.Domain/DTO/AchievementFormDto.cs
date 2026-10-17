using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.DTO
{
    public class AchievementFormDto
    {
        [StringLength(100)]
        [Required]
        public string? Name { get; set; }
        [StringLength(500)]
        public string? Description { get; set; }
    }
}
