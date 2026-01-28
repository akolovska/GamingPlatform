using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.DTO
{
    public class ReviewDto
    {
        public string? Comment { get; set; }
        public int Score { get; set; }
        public DateTime? DateRated { get; set; }
        public string? GamerName { get; set; }
    }

}
