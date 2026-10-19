using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces
{
    public interface IFriendService
    {
        FriendRequestResult SendFriendRequest(string userId, string targetUserName);
        void CancelFriendRequest(string userId, string targetUserId);
        void AcceptFriendRequest(Guid requestId, string userId);
        void DeclineFriendRequest(Guid requestId, string userId);
        void RemoveFriend(string userId, string friendId);
        void BlockUser(string userId, string targetUserId);
        void UnblockUser(string userId, string targetUserId);

        List<UserSummaryDto> GetFriends(string userId);
        List<PendingRequestDto> GetPendingRequests(string userId);
        List<UserSummaryDto> GetBlockedUsers(string userId);
        bool IsBlockedEitherWay(string a, string b);
        Friend? GetPendingBetween(string userA, string userB);
        bool CanViewWishlist(GamingPlatformUser owner, string viewerId);

        // Used when an account is deleted
        void RemoveAllFor(string userId);
    }
}
