using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class Library : BaseEntity
    {
        public Guid? GamerId { get; set; }
        public Gamer? Gamer { get; set; }
        public Guid? GameId { get; set; }
        public Game? Game { get; set; }
        public DateTime? DateAdded { get; set; } // When the game was added to the gamer's library
        public int PlayTimeHours { get; set; } // Total playtime in hours
    }
}
