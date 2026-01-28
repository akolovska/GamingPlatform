using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class Wishlist : BaseEntity
    {
        public string? GamerId { get; set; }
        public Guid? GameId { get; set; }
    }
    
}
