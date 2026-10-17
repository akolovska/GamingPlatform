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
        
    }
}
