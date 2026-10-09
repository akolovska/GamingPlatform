using System.ComponentModel.DataAnnotations;

namespace GamingPlatform.Domain.Enum
{
    public enum WishlistVisibility
    {
        [Display(Name = "Friends only")] FriendsOnly = 0, // first, so existing users get this default
        [Display(Name = "Public")] Public = 1,
        [Display(Name = "Private")] Private = 2
    }
}