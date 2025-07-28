using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models
{
    public class GamerFriend : BaseEntity
    {
        public Guid? GamerId { get; set; }
        public Gamer? Gamer { get; set; }

        public Guid? FriendId { get; set; }
        public Gamer? Friend { get; set; }

        public DateTime? AddedOn { get; set; }
    }
}