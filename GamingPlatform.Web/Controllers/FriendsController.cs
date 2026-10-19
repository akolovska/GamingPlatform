using GamingPlatform.Domain.Enum;
using GamingPlatform.Service.Interfaces;
using GamingPlatform.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    [Authorize, RequireVerifiedEmail]
    public class FriendsController : AppController
    {
        private readonly IFriendService _friendService;

        public FriendsController(IFriendService friendService)
        {
            _friendService = friendService;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AddFriend(string username)
        {
            var result = _friendService.SendFriendRequest(UserId!, username);

            if (result is FriendRequestResult.Sent or FriendRequestResult.Accepted)
                TempData["Success"] = result == FriendRequestResult.Sent
                    ? "Friend request sent!"
                    : $"{username} had already sent you a request. You are now friends!";
            else
                TempData["Error"] = result switch
                {
                    FriendRequestResult.Self => "You can't add yourself as a friend.",
                    FriendRequestResult.AlreadyFriends => "You are already friends.",
                    FriendRequestResult.AlreadyPending => "Request is already pending.",
                    FriendRequestResult.YouBlocked => "You have blocked this user. Unblock them first.",
                    _ => "There is no user with that username."
                };

            return BackToProfiles();
        }

        // userId is the other user's id in the actions below; the signed-in user always comes from the cookie

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CancelRequest(string userId)
        {
            _friendService.CancelFriendRequest(UserId!, userId);
            TempData["Success"] = "Friend request cancelled.";
            return BackToProfiles();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AcceptFriend(Guid requestId)
        {
            _friendService.AcceptFriendRequest(requestId, UserId!);
            TempData["Success"] = "Friend request accepted!";
            return BackToProfiles();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeclineFriend(Guid requestId)
        {
            _friendService.DeclineFriendRequest(requestId, UserId!);
            TempData["Success"] = "Friend request declined.";
            return BackToProfiles();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RemoveFriend(string userId)
        {
            _friendService.RemoveFriend(UserId!, userId);
            TempData["Success"] = "Friend removed.";
            return BackToProfiles();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult BlockUser(string userId)
        {
            _friendService.BlockUser(UserId!, userId);
            TempData["Success"] = "User blocked.";
            return BackToProfiles();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult UnblockUser(string userId)
        {
            _friendService.UnblockUser(UserId!, userId);
            TempData["Success"] = "User unblocked.";
            return BackToProfiles();
        }

        private IActionResult BackToProfiles() => RedirectToAction("Index", "Account");
    }
}
