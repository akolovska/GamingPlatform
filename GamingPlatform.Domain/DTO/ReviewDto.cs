using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.DTO
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public string? Comment { get; set; }
        public int Score { get; set; }
        public DateTime? DateRated { get; set; }
        public string? GamerName { get; set; }
        public bool IsMine { get; set; }

    }

}
