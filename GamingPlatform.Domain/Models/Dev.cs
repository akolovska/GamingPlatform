using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class Dev : BaseEntity
    {
        public string? Username { get; set; }
        public string? Description { get; set; }
        public string? ProfilePicture { get; set; }
        public DateTime? DateJoined { get; set; }
        public string? Email { get; set; }
        public string? StudioName { get; set; }
        public ICollection<GameDev> GameDevs { get; set; }
    }
}
