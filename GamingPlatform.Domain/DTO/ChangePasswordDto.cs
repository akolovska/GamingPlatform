using System.ComponentModel.DataAnnotations;

namespace GamingPlatform.Domain.DTO
{
    public class ChangePasswordDto
    {
        [Required, DataType(DataType.Password), Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password), Display(Name = "Confirm new password")]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
