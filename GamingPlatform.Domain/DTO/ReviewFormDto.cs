using System.ComponentModel.DataAnnotations;

namespace GamingPlatform.Domain.DTO
{
    public class ReviewFormDto
    {
        [Required, StringLength(1000)]
        public string? Comment { get; set; }

        [Required, Range(1, 5)]
        public int Score { get; set; }
    }
}
