using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamingPlatform.Web.Controllers
{
    [Authorize]
    public class FriendsController : Controller
    {
        private readonly IFriendService _friendService;
        private readonly UserManager<GamingPlatformUser> _userManager;

        public FriendsController(IFriendService friendService, UserManager<GamingPlatformUser> userManager)
        {
            _friendService = friendService;
            _userManager = userManager;
        }

        // Returns the user only if logged in AND email confirmed
        private async Task<GamingPlatformUser?> GetVerifiedUserAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Verified"] = "Please verify your email to access all features.";
                return null;
            }
            return user;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFriend(string username)
        {
            var user = await GetVerifiedUserAsync();
            if (user == null) return RedirectToAction("Index", "Account");

            var target = await _userManager.FindByNameAsync(username?.Trim() ?? "");
            var result = target == null
                ? FriendRequestResult.UserNotFound
                : _friendService.SendFriendRequest(user.Id, target.Id);

            switch (result)
            {
                case FriendRequestResult.Sent:
                    TempData["FriendMessage"] = "Friend request sent!";
                    break;
                case FriendRequestResult.Accepted:
                    TempData["FriendMessage"] = $"{target!.UserName} had already sent you a request. You are now friends!";
                    break;
                default:
                    TempData["Error"] = result switch
                    {
                        FriendRequestResult.UserNotFound => "User does not exist.",
                        FriendRequestResult.Self => "You can't add yourself as a friend.",
                        FriendRequestResult.AlreadyFriends => "You are already friends.",
                        FriendRequestResult.AlreadyPending => "Request is already pending.",
                        FriendRequestResult.YouBlocked => "You have blocked this user. Unblock them first.",
                        _ => "You can't send a friend request to this user." // blocked by them: don't reveal it
                    };
                    break;
            }
            return RedirectToAction("Index", "Account");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(string requestId)
        {
            var user = await GetVerifiedUserAsync();
            if (user == null) return RedirectToAction("Index", "Account");

            _friendService.CancelFriendRequest(user.Id, requestId);
            TempData["FriendMessage"] = "Friend request cancelled.";
            return RedirectToAction("Index", "Account");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptFriend(Guid requestId)
        {
            var user = await GetVerifiedUserAsync();
            if (user == null) return RedirectToAction("Index", "Account");

            _friendService.AcceptFriendRequest(requestId, user.Id);
            TempData["FriendMessage"] = "Friend request accepted!";
            return RedirectToAction("Index", "Account");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineFriend(Guid requestId)
        {
            var user = await GetVerifiedUserAsync();
            if (user == null) return RedirectToAction("Index", "Account");

            _friendService.DeclineFriendRequest(requestId, user.Id);
            TempData["FriendMessage"] = "Friend request declined.";
            return RedirectToAction("Index", "Account");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFriend(string requestId)
        {
            var user = await GetVerifiedUserAsync();
            if (user == null) return RedirectToAction("Index", "Account");

            _friendService.RemoveFriend(user.Id, requestId);
            TempData["FriendMessage"] = "Friend removed.";
            return RedirectToAction("Index", "Account");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockFriend(string requestId)
        {
            var user = await GetVerifiedUserAsync();
            if (user == null) return RedirectToAction("Index", "Account");

            _friendService.BlockFriend(user.Id, requestId);
            TempData["FriendMessage"] = "User blocked.";
            return RedirectToAction("Index", "Account");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(string requestId)
        {
            var user = await GetVerifiedUserAsync();
            if (user == null) return RedirectToAction("Index", "Account");

            _friendService.UnblockUser(user.Id, requestId);
            TempData["FriendMessage"] = "User unblocked.";
            return RedirectToAction("Index", "Account");
        }
    }
}
