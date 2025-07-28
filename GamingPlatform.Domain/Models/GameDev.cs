using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class GameDev : BaseEntity
    {
        public Guid? GameId { get; set; }
        public Game? Game { get; set; }
        public Guid? DevId { get; set; }
        public Dev? Dev { get; set; }
    }
}
