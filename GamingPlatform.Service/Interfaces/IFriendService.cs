using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Service.Interfaces
{
    public interface IFriendService
    {
        FriendRequestResult SendFriendRequest(string userId, string targetUserId);
        void CancelFriendRequest(string userId, string targetUserId);
        void AcceptFriendRequest(Guid requestId, string userId);
        void DeclineFriendRequest(Guid requestId, string userId);
        List<GamingPlatformUser> GetFriends(string userId);
        List<Friend> GetPendingRequests(string userId);
        void RemoveFriend(string userId, string friendId);
        void BlockFriend(string userId, string targetUserId);
        void UnblockUser(string userId, string targetUserId);
        bool IsBlocked(string userId, string otherId);        // userId has blocked otherId
        bool IsBlockedEitherWay(string a, string b);
        List<GamingPlatformUser> GetBlockedUsers(string userId);
        Friend? GetPendingBetween(string userA, string userB);
        bool CanViewWishlist(GamingPlatformUser owner, string viewerId);
    }
}
