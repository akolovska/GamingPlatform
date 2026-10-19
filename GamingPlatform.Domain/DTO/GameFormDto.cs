using System.ComponentModel.DataAnnotations;

namespace GamingPlatform.Domain.DTO
{
    // The cover file is posted separately, and the cover URL is never taken from the form
    public class GameFormDto
    {
        public Guid Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        public List<Guid> GenreIds { get; set; } = new();

        [Display(Name = "Release date")]
        public DateTime? ReleaseDate { get; set; }

        [Range(0, 10000)]
        public decimal? Price { get; set; }

        [StringLength(100)]
        public string? Platform { get; set; }

        [StringLength(2000), Display(Name = "System requirements")]
        public string? SystemRequirements { get; set; }

        [StringLength(200), Display(Name = "Trailer (YouTube link)")]
        public string? TrailerUrl { get; set; }

        [Range(0, 90), Display(Name = "Discount %")]
        public int DiscountPercent { get; set; }

        [Display(Name = "Sale starts")]
        public DateTime? DiscountStartsAt { get; set; }

        [Display(Name = "Sale ends")]
        public DateTime? DiscountEndsAt { get; set; }

        [Display(Name = "Studio")]
        public Guid? DevId { get; set; } // only used by admins on Create
    }
}
