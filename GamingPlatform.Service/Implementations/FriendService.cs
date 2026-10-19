using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace GamingPlatform.Service.Implementations
{
    public class FriendService : IFriendService
    {
        private readonly IRepository<Friend> _friendRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notifications;
        private readonly UserManager<GamingPlatformUser> _userManager;

        public FriendService(
            IRepository<Friend> friendRepository,
            IUnitOfWork unitOfWork,
            INotificationService notifications,
            UserManager<GamingPlatformUser> userManager)
        {
            _friendRepository = friendRepository;
            _unitOfWork = unitOfWork;
            _notifications = notifications;
            _userManager = userManager;
        }

        private string UserName(string userId) =>
            _userManager.Users.Where(u => u.Id == userId).Select(u => u.UserName).FirstOrDefault() ?? "Someone";

        // Tells the person who sent the request that it was accepted
        private void NotifyAccepted(Friend request)
        {
            var name = UserName(request.FriendId);
            _notifications.Notify(request.UserId, $"{name} accepted your friend request.", $"/Account/Details?username={Uri.EscapeDataString(name)}");
        }

        // All rows between two users, in either direction
        private List<Friend> Between(string a, string b) =>
            _friendRepository.GetAll(f => f, predicate: f =>
                (f.UserId == a && f.FriendId == b) ||
                (f.UserId == b && f.FriendId == a));

        public FriendRequestResult SendFriendRequest(string userId, string targetUserName)
        {
            var normalized = _userManager.NormalizeName(targetUserName?.Trim() ?? string.Empty);
            var target = _userManager.Users.FirstOrDefault(u => u.NormalizedUserName == normalized);
            if (target == null) return FriendRequestResult.UserNotFound;
            if (target.Id == userId) return FriendRequestResult.Self;

            var existing = Between(userId, target.Id);

            if (existing.Any(f => f.UserId == userId && f.Status == FriendshipStatus.Blocked))
                return FriendRequestResult.YouBlocked;
            // Being blocked looks the same as the user not existing, so a block can't be detected
            if (existing.Any(f => f.UserId == target.Id && f.Status == FriendshipStatus.Blocked))
                return FriendRequestResult.UserNotFound;
            if (existing.Any(f => f.Status == FriendshipStatus.Accepted))
                return FriendRequestResult.AlreadyFriends;

            // They already asked me -> just accept
            var incoming = existing.FirstOrDefault(f => f.UserId == target.Id && f.Status == FriendshipStatus.Pending);
            if (incoming != null)
            {
                Accept(incoming);
                NotifyAccepted(incoming);
                return FriendRequestResult.Accepted;
            }

            if (existing.Any(f => f.UserId == userId && f.Status == FriendshipStatus.Pending))
                return FriendRequestResult.AlreadyPending;

            // The unique (UserId, FriendId) index rejects a double submit
            if (!_friendRepository.TryInsert(new Friend(userId, target.Id, FriendshipStatus.Pending, DateTime.UtcNow)))
                return FriendRequestResult.AlreadyPending;

            _notifications.Notify(target.Id, $"{UserName(userId)} sent you a friend request.", "/Account#requests");
            return FriendRequestResult.Sent;
        }

        // Sender cancels their own pending request
        public void CancelFriendRequest(string userId, string targetUserId)
        {
            var request = Between(userId, targetUserId)
                .FirstOrDefault(f => f.UserId == userId && f.Status == FriendshipStatus.Pending);
            if (request != null) _friendRepository.Delete(request);
        }

        public void AcceptFriendRequest(Guid requestId, string userId)
        {
            var request = _friendRepository.Get(x => x, x => x.Id == requestId);
            if (request != null && request.FriendId == userId && request.Status == FriendshipStatus.Pending)
            {
                Accept(request);
                NotifyAccepted(request);
            }
        }

        // Accepts the request and removes a reverse pending row, which can exist if
        // both users clicked "Add friend" at the same moment
        private void Accept(Friend request) => _unitOfWork.InTransaction(() =>
        {
            request.Status = FriendshipStatus.Accepted;
            _friendRepository.Update(request);

            var reverse = Between(request.UserId, request.FriendId)
                .Where(f => f.Id != request.Id && f.Status == FriendshipStatus.Pending)
                .ToList();
            if (reverse.Count > 0) _friendRepository.DeleteRange(reverse);
        });

        public void DeclineFriendRequest(Guid requestId, string userId)
        {
            var request = _friendRepository.Get(x => x, x => x.Id == requestId);
            if (request != null && request.FriendId == userId && request.Status == FriendshipStatus.Pending)
                _friendRepository.Delete(request);
        }

        public void RemoveFriend(string userId, string friendId)
        {
            var rows = Between(userId, friendId).Where(f => f.Status == FriendshipStatus.Accepted).ToList();
            if (rows.Count > 0) _friendRepository.DeleteRange(rows);
        }

        public void BlockUser(string userId, string targetUserId)
        {
            if (userId == targetUserId) return;
            if (!_userManager.Users.Any(u => u.Id == targetUserId)) return;

            _unitOfWork.InTransaction(() =>
            {
                var rows = Between(userId, targetUserId);
                bool alreadyBlocked = rows.Any(f => f.UserId == userId && f.Status == FriendshipStatus.Blocked);

                // Remove friendship/pending rows, keep any existing blocks
                var toRemove = rows.Where(f => f.Status != FriendshipStatus.Blocked).ToList();
                if (toRemove.Count > 0) _friendRepository.DeleteRange(toRemove);

                if (!alreadyBlocked)
                    _friendRepository.TryInsert(new Friend(userId, targetUserId, FriendshipStatus.Blocked, DateTime.UtcNow));
            });
        }

        public void UnblockUser(string userId, string targetUserId)
        {
            var row = Between(userId, targetUserId)
                .FirstOrDefault(f => f.UserId == userId && f.Status == FriendshipStatus.Blocked);
            if (row != null) _friendRepository.Delete(row);
        }

        public List<UserSummaryDto> GetFriends(string userId)
        {
            var ids = _friendRepository.GetAll(
                selector: f => f.UserId == userId ? f.FriendId : f.UserId,
                predicate: f => f.Status == FriendshipStatus.Accepted && (f.UserId == userId || f.FriendId == userId));
            return Summaries(ids);
        }

        public List<PendingRequestDto> GetPendingRequests(string userId) =>
            _friendRepository.GetAll(
                selector: f => new PendingRequestDto { Id = f.Id, SenderUsername = f.User.UserName ?? "Unknown" },
                predicate: f => f.FriendId == userId && f.Status == FriendshipStatus.Pending,
                orderBy: q => q.OrderByDescending(f => f.CreatedAt));

        public List<UserSummaryDto> GetBlockedUsers(string userId)
        {
            var ids = _friendRepository.GetAll(
                selector: f => f.FriendId,
                predicate: f => f.UserId == userId && f.Status == FriendshipStatus.Blocked);
            return Summaries(ids);
        }

        public bool IsBlockedEitherWay(string a, string b) =>
            _friendRepository.Any(f => f.Status == FriendshipStatus.Blocked &&
                ((f.UserId == a && f.FriendId == b) || (f.UserId == b && f.FriendId == a)));

        public Friend? GetPendingBetween(string userA, string userB) =>
            Between(userA, userB).FirstOrDefault(f => f.Status == FriendshipStatus.Pending);

        public bool CanViewWishlist(GamingPlatformUser owner, string viewerId)
        {
            if (owner.Id == viewerId) return true;                   // your own wishlist
            if (IsBlockedEitherWay(owner.Id, viewerId)) return false; // blocked users never see it

            return owner.WishlistVisibility switch
            {
                WishlistVisibility.Public => true,
                WishlistVisibility.FriendsOnly => Between(owner.Id, viewerId).Any(f => f.Status == FriendshipStatus.Accepted),
                _ => false                                            // Private
            };
        }

        public void RemoveAllFor(string userId)
        {
            var rows = _friendRepository.GetAll(f => f, f => f.UserId == userId || f.FriendId == userId);
            if (rows.Count > 0) _friendRepository.DeleteRange(rows);
        }

        private List<UserSummaryDto> Summaries(List<string> userIds) =>
            _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .OrderBy(u => u.UserName)
                .Select(u => new UserSummaryDto { Id = u.Id, UserName = u.UserName ?? "Unknown", ProfilePictureUrl = u.ProfilePictureUrl })
                .ToList();
    }
}
