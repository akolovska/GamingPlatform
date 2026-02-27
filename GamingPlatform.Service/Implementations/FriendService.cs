using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
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
        public bool SendFriendRequest(string userId, Guid friendUsername)
        {
            // Find the user by username
            var friend = _userManager.Users.FirstOrDefault(u => u.UserName.Equals(friendUsername));
            if (friend == null || friend.Id == userId)
                return false;

            // Check if a friend request or friendship already exists
            bool exists = _friendRepository.GetAll(f => f).Any(f =>
                (f.UserId == userId && f.FriendId == friend.Id) ||
                (f.UserId == friend.Id && f.FriendId == userId));

            if (exists) return false;

            // Create new friend request
            _friendRepository.Insert(new Friend(userId,
                friend.Id,
                FriendshipStatus.Pending,
                DateTime.UtcNow));

            return true;
        }

        public void AcceptFriendRequest(Guid friendRequestId, string userId)
        {
            var request = _friendRepository.Get(
                selector: x => x,
                predicate: x => x.Id == friendRequestId
            );

            if (request != null && request.FriendId == userId && request.Status == FriendshipStatus.Pending)
            {
                request.Status = FriendshipStatus.Accepted;
                _friendRepository.Update(request);
            }
        }



        public List<GamingPlatformUser> GetFriends(string userId)
        {
            // Get all accepted friends
            var acceptedFriendIds = _friendRepository.GetAll(f => f)
                .Where(f => f.Status == FriendshipStatus.Accepted &&
                            (f.UserId == userId || f.FriendId == userId))
                .Select(f => f.UserId == userId ? f.FriendId : f.UserId)
                .ToList();

            // Use UserManager.Users to query users
            return _userManager.Users
                .Where(u => acceptedFriendIds.Contains(u.Id))
                .ToList();
        }



        public List<Friend> GetPendingRequests(string userId)
        {
            return _friendRepository.GetAll(f => f)
                .Where(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending)
                .ToList();
        }

        public void RemoveFriend(string userId, Guid friendId)
        {
            Friend friendship = _friendRepository.Get(
                selector: x => x,
                predicate: x =>
                    x.UserId == userId && x.FriendId.Equals(friendId)
            );
            _friendRepository.Delete(friendship);
        }

        public void BlockFriend(string userId, Guid friendId)
        {
            Friend? friendship = _friendRepository.Get(
                selector: x => x,
                predicate: x =>
                    x.UserId == userId && x.FriendId.Equals(friendId)
            );
            if (friendship == null)
            {
                Friend newFriendship = new Friend(userId, friendId.ToString(), FriendshipStatus.Blocked, DateTime.Now);
                _friendRepository.Insert(newFriendship);
            }
            else
            {
                friendship.Status = FriendshipStatus.Blocked;
                _friendRepository.Update(friendship);
            }
        }

        public bool IsBlocked(string userId, Guid friendId)
        {
            return _friendRepository.GetAll(f => f)
                .Any(f => f.UserId == userId && f.FriendId.Equals(friendId) && f.Status == FriendshipStatus.Blocked);
        }
    }
}
