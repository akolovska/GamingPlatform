using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Email;
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
        private readonly IGameService _gameService;

        public AccountController(UserManager<GamingPlatformUser> userManager, IFriendService friendService, IGameService gameService)
        {
            _userManager = userManager;
            _friendService = friendService;
            _gameService = gameService;
        }
        public async Task<IActionResult> Edit()
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();
            
            var friends = _friendService.GetFriends(user.Id);
            ViewBag.Friends = friends;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GamingPlatformUser model)
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();
            if (!_userManager.IsEmailConfirmedAsync(user).Result)
            {
                TempData["Verified"] = "Please verify your email to access all features.";
                ViewBag.Friends = _friendService.GetFriends(user.Id);
                return View(model);
            }

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
            ViewBag.PendingRequests = pendingRequests.Select(r => new
            {
                Id = r.Id,
                SenderUsername = _userManager.Users.FirstOrDefault(u => u.Id == r.UserId)?.UserName
            }).ToList();


            return View(model);
        }
        public IActionResult Details(string requestId)
        {
            var loggedInUser = _userManager.GetUserAsync(User).Result;
            var user = _userManager.Users
                .FirstOrDefault(u => u.UserName == requestId || u.Email == requestId);
            if (user == null) return NotFound();
            if (_friendService.IsBlocked(loggedInUser.Id, Guid.Parse(user.Id)))
            {
                TempData["Blocked"] = "This user is blocked.";
                return RedirectToAction(nameof(Index));
            }
            
            var friends = _friendService.GetFriends(user.Id);
            ViewBag.Friends = friends;
            bool isFriend = friends.Any(f => f.Id == loggedInUser.Id);
            bool isBlocked = _friendService.IsBlocked(loggedInUser.Id, Guid.Parse(user.Id));
            ViewBag.Blocked = isBlocked;
            ViewBag.IsFriend = isFriend;
            var games = _gameService.GetGameById(Guid.Parse(user.Id));
            ViewBag.Games = games;
            ViewBag.User = loggedInUser;
            return View(user);
        }
       
    }
}
