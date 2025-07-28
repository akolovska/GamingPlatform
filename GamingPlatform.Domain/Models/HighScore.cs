using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class HighScore : BaseEntity
    {
        public Guid? GamerId { get; set; }
        public Gamer? Gamer { get; set; }
        public Guid? GameId { get; set; }
        public Game? Game { get; set; }
        public int Score { get; set; }
        public DateTime DateAchieved { get; set; }
    }
}
