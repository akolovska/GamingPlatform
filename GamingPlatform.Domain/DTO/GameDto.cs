using GamingPlatform.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.DTO
{
    public class GameDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
        public DateTime? ReleaseDate { get; set; }
        public decimal? Price { get; set; }
        public string? Platform { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? SystemRequirements { get; set; }

        public List<DevDto> Developers { get; set; }
        public List<AchievementDto> Achievements { get; set; }
        public double? Rating { get; set; }
        public bool InWishlist { get; set; }
        public bool InLibrary { get; set; }
        public List<ReviewDto> Reviews { get; set; }
    }
}
