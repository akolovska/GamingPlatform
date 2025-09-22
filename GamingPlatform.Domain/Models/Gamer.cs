using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class Gamer : BaseEntity
    {
        public string? Username { get; set; }
        public string? Description { get; set; }
        public string? ProfilePicture { get; set; }
        public DateTime? DateJoined { get; set; }
        public string? Email { get; set; }
        public ICollection<HighScore> HighScores { get; set; }
        public ICollection<Library> Library { get; set; }
        public ICollection<Wishlist> Wishlist { get; set; }
        public ICollection<Gamer> Friends { get; set; }
    }
}
