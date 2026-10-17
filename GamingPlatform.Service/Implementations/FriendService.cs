using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GamingPlatform.Service.Implementations
{
    public class FriendService : IFriendService
    {
        private readonly IRepository<Friend> _friendRepository;
        private readonly UserManager<GamingPlatformUser> _userManager;

        public FriendService(IRepository<Friend> friendRepository, UserManager<GamingPlatformUser> userManager)
        {
            _friendRepository = friendRepository;
            _userManager = userManager;
        }

        // All rows between two users, in either direction
        private List<Friend> Between(string a, string b) =>
            _friendRepository.GetAll(f => f, predicate: f =>
                    (f.UserId == a && f.FriendId == b) ||
                    (f.UserId == b && f.FriendId == a))
                .ToList();

        public FriendRequestResult SendFriendRequest(string userId, string targetUserId)
        {
            if (userId == targetUserId) return FriendRequestResult.Self;
            if (!_userManager.Users.Any(u => u.Id == targetUserId)) return FriendRequestResult.UserNotFound;

            var existing = Between(userId, targetUserId);

            if (existing.Any(f => f.UserId == userId && f.Status == FriendshipStatus.Blocked))
                return FriendRequestResult.YouBlocked;
            if (existing.Any(f => f.UserId == targetUserId && f.Status == FriendshipStatus.Blocked))
                return FriendRequestResult.BlockedByThem;
            if (existing.Any(f => f.Status == FriendshipStatus.Accepted))
                return FriendRequestResult.AlreadyFriends;

            // They already asked me -> just accept
            var incoming = existing.FirstOrDefault(f =>
                f.UserId == targetUserId && f.Status == FriendshipStatus.Pending);
            if (incoming != null)
            {
                Accept(incoming);
                return FriendRequestResult.Accepted;
            }

            if (existing.Any(f => f.UserId == userId && f.Status == FriendshipStatus.Pending))
                return FriendRequestResult.AlreadyPending;

            try
            {
                _friendRepository.Insert(new Friend(userId, targetUserId, FriendshipStatus.Pending, DateTime.UtcNow));
            }
            catch (DbUpdateException)
            {
                // Double submit: the unique (UserId, FriendId) index rejected the second insert
                return FriendRequestResult.AlreadyPending;
            }
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
                Accept(request);
        }

        // Accepts the request and removes a reverse pending row, which can exist if
        // both users clicked "Add friend" at the same moment
        private void Accept(Friend request)
        {
            request.Status = FriendshipStatus.Accepted;
            _friendRepository.Update(request);

            foreach (var row in Between(request.UserId, request.FriendId)
                         .Where(f => f.Id != request.Id && f.Status == FriendshipStatus.Pending))
                _friendRepository.Delete(row);
        }

        public void DeclineFriendRequest(Guid requestId, string userId)
        {
            var request = _friendRepository.Get(x => x, x => x.Id == requestId);
            if (request != null && request.FriendId == userId && request.Status == FriendshipStatus.Pending)
                _friendRepository.Delete(request);
        }

        public List<GamingPlatformUser> GetFriends(string userId)
        {
            var ids = _friendRepository.GetAll(f => f, predicate: f =>
                    f.Status == FriendshipStatus.Accepted &&
                    (f.UserId == userId || f.FriendId == userId))
                .Select(f => f.UserId == userId ? f.FriendId : f.UserId)
                .ToList();

            return _userManager.Users.Where(u => ids.Contains(u.Id)).ToList();
        }

        public List<Friend> GetPendingRequests(string userId) =>
            _friendRepository.GetAll(f => f, predicate: f =>
                    f.FriendId == userId && f.Status == FriendshipStatus.Pending)
                .ToList();

        public void RemoveFriend(string userId, string friendId)
        {
            foreach (var row in Between(userId, friendId).Where(f => f.Status == FriendshipStatus.Accepted))
                _friendRepository.Delete(row);
        }

        public void BlockFriend(string userId, string targetUserId)
        {
            if (userId == targetUserId) return;
            if (!_userManager.Users.Any(u => u.Id == targetUserId)) return;

            var rows = Between(userId, targetUserId);
            bool alreadyBlocked = rows.Any(f => f.UserId == userId && f.Status == FriendshipStatus.Blocked);

            // Remove friendship/pending rows, keep any existing blocks
            foreach (var row in rows.Where(f => f.Status != FriendshipStatus.Blocked))
                _friendRepository.Delete(row);

            if (!alreadyBlocked)
                _friendRepository.Insert(new Friend(userId, targetUserId, FriendshipStatus.Blocked, DateTime.UtcNow));
        }

        public void UnblockUser(string userId, string targetUserId)
        {
            var row = Between(userId, targetUserId)
                .FirstOrDefault(f => f.UserId == userId && f.Status == FriendshipStatus.Blocked);
            if (row != null) _friendRepository.Delete(row);
        }

        public bool IsBlocked(string userId, string otherId) =>
            _friendRepository.GetAll(f => f, predicate: f =>
                    f.UserId == userId && f.FriendId == otherId && f.Status == FriendshipStatus.Blocked)
                .Any();

        public bool IsBlockedEitherWay(string a, string b) =>
            IsBlocked(a, b) || IsBlocked(b, a);

        public List<GamingPlatformUser> GetBlockedUsers(string userId)
        {
            var ids = _friendRepository.GetAll(f => f, predicate: f =>
                    f.UserId == userId && f.Status == FriendshipStatus.Blocked)
                .Select(f => f.FriendId)
                .ToList();

            return _userManager.Users
                .Where(u => ids.Contains(u.Id))
                .OrderBy(u => u.UserName)
                .ToList();
        }
        public Friend? GetPendingBetween(string userA, string userB) =>
            Between(userA, userB).FirstOrDefault(f => f.Status == FriendshipStatus.Pending);

        // FriendService
        public bool CanViewWishlist(GamingPlatformUser owner, string viewerId)
        {
            if (owner.Id == viewerId) return true;                   // your own wishlist
            if (IsBlockedEitherWay(owner.Id, viewerId)) return false; // blocked users never see it

            return owner.WishlistVisibility switch
            {
                WishlistVisibility.Public => true,
                WishlistVisibility.FriendsOnly => Between(owner.Id, viewerId)
                    .Any(f => f.Status == FriendshipStatus.Accepted),
                _ => false                                            // Private
            };
        }
    }
}
