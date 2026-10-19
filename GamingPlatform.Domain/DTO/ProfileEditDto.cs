using System.ComponentModel.DataAnnotations;
using GamingPlatform.Domain.Enum;

namespace GamingPlatform.Domain.DTO
{
    public class ProfileEditDto
    {
        [Required, StringLength(50), Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50), Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Bio { get; set; }

        [Display(Name = "Who can see my wishlist")]
        public WishlistVisibility WishlistVisibility { get; set; }

        [Display(Name = "Email me when a game on my wishlist goes on sale")]
        public bool WishlistSaleEmails { get; set; }
    }
}
