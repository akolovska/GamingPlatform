using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class GameGenre : BaseEntity
    {
        public Guid GameId { get; set; }
        public Game Game { get; set; } = null!;
        public Guid GenreId { get; set; }
        public Genre Genre { get; set; } = null!;
    }
}
