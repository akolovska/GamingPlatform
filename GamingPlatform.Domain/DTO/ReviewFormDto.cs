using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GamingPlatform.Domain.DTO
{
    public class ReviewFormDto
    {
        [StringLength(1000)]
        [Required]
        public string? Comment { get; set; }
        [Range(1, 5)]
        [Required]
        public int Score { get; set; }
    }
}
