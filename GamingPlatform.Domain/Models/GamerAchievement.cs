using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class GamerAchievement : BaseEntity
	{
		public Guid? GamerId { get; set; }
		public Gamer? Gamer { get; set; }
		public Guid? AchievementId { get; set; }
		public Achievement? Achievement { get; set; }
		public DateTime? DateUnlocked { get; set; }
}
}
