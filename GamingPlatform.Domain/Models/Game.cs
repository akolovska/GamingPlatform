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
        public string? Platform { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? SystemRequirements { get; set; }
        public ICollection<Library> Libraries { get; set; }
        public ICollection<GameDev> GameDevelopers { get; set; }
        public ICollection<Wishlist> Wishlists { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<Achievement> Achievements { get; set; }
    }
}
