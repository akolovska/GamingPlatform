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
            bool ok = target != null && _friendService.SendFriendRequest(user.Id, target.Id);

            // Same message either way, so people can't probe which usernames exist
            TempData["FriendMessage"] = ok ? "Friend request sent!" : "Unable to send request.";
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
