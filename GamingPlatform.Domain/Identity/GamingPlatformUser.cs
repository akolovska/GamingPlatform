using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Identity
{
    public class GamingPlatformUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public ICollection<Friend> FriendshipsSent { get; set; }
        public ICollection<Friend> FriendshipsReceived { get; set; }
        public WishlistVisibility WishlistVisibility { get; set; } = WishlistVisibility.FriendsOnly;
    }
}
