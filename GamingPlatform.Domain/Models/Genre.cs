using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class Genre : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
    }
}
