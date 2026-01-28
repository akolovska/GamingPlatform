using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<GamingPlatformUser> _userManager;
        private readonly IFriendService _friendService;

        public AccountController(UserManager<GamingPlatformUser> userManager, IFriendService friendService)
        {
            _userManager = userManager;
            _friendService = friendService;
        }

        [Authorize]
        public IActionResult Index()
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();
            var friends = _friendService.GetFriends(user.Id);
            ViewBag.Friends = friends;
            return View(user);
        }
        public IActionResult Edit()
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(GamingPlatformUser model)
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();

            // Update fields
            user.Email = model.Email;
            user.Bio = model.Bio;
            user.ProfilePictureUrl = model.ProfilePictureUrl;

            var result = _userManager.UpdateAsync(user).Result; // sync call

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            var pendingRequests = _friendService.GetPendingRequests(user.Id); // implement this
            ViewBag.PendingRequests = pendingRequests.Select(r => new {
                Id = r.Id,
                SenderUsername = _userManager.Users.FirstOrDefault(u => u.Id == r.UserId)?.UserName
            }).ToList();


            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddFriend(string friendUsername)
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();

            bool success = _friendService.SendFriendRequest(user.Id, friendUsername);
            TempData["FriendMessage"] = success ? "Friend request sent!" : "Unable to send request.";

            return RedirectToAction(nameof(Index));
        }

    }
}
