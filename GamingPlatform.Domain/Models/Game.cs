using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class Game : BaseEntity
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Genre { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public decimal? Price { get; set; }
        public string? Platform { get; set; } // e.g., PC, Console, Mobile
        public string? CoverImageUrl { get; set; } // URL to the game's cover image
        public ICollection<HighScore> HighScores { get; set; }
        public ICollection<Library> Library { get; set; }
        public ICollection<GameDev> GameDevelopers { get; set; }
        public ICollection<Wishlist> Wishlist { get; set; }
    }
}
