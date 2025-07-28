using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class Dev : BaseEntity
    {
        public string? StudioName { get; set; }
        public ICollection<GameDev> GameDevs { get; set; }
    }
}
