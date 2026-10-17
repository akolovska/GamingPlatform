using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;

namespace GamingPlatform.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxBytes = 2 * 1024 * 1024; // 2 MB

        private readonly UserManager<GamingPlatformUser> _userManager;
        private readonly IFriendService _friendService;
        private readonly IGameService _gameService;
        private readonly ILibraryService _libraryService;
        private readonly IWishlistService _wishlistService;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<GamingPlatformUser> userManager,
            IFriendService friendService,
            IGameService gameService,
            ILibraryService libraryService,
            IWishlistService wishlistService,
            IWebHostEnvironment env,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _friendService = friendService;
            _gameService = gameService;
            _libraryService = libraryService;
            _wishlistService = wishlistService;
            _env = env;
            _emailSender = emailSender;
        }

        // ---------- My account ----------

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var pending = _friendService.GetPendingRequests(user.Id);
            var senderIds = pending.Select(r => r.UserId).ToList();
            var names = _userManager.Users
                .Where(u => senderIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.UserName ?? "Unknown");

            ViewBag.PendingRequests = pending.Select(r => new PendingRequestDto
            {
                Id = r.Id,
                SenderUsername = names.GetValueOrDefault(r.UserId, "Unknown")
            }).ToList();

            ViewBag.IsVerified =await _userManager.IsEmailConfirmedAsync(user);
            ViewBag.BlockedUsers = _friendService.GetBlockedUsers(user.Id);
            ViewBag.Friends = _friendService.GetFriends(user.Id);
            ViewBag.Games = BuildGameDtos(_libraryService.GetUserGames(user.Id), user.Id);
            return View(user);
        }

        // ---------- Edit profile ----------

        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            ViewBag.Friends = _friendService.GetFriends(user.Id);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GamingPlatformUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // The posted model only contains Email and Bio, so restore the picture
            // for any path where we re-render the view.
            model.ProfilePictureUrl = user.ProfilePictureUrl;

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Verified"] = "Please verify your email to access all features.";
                ViewBag.Friends = _friendService.GetFriends(user.Id);
                return View(model);
            }

            // Only the fields this form may change. ProfilePictureUrl is
            // changed only by UploadAvatar / RemoveAvatar.
            user.Email = model.Email;
            user.Bio = model.Bio;
            if (Enum.IsDefined(typeof(WishlistVisibility), model.WishlistVisibility))
                user.WishlistVisibility = model.WishlistVisibility;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return RedirectToAction(nameof(Index));

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.Friends = _friendService.GetFriends(user.Id);
            return View(model);
        }

        // ---------- Avatar ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (avatarFile == null || avatarFile.Length == 0)
            {
                TempData["AvatarError"] = "Please choose a file.";
                return RedirectToAction(nameof(Edit));
            }

            var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext) || avatarFile.Length > MaxBytes)
            {
                TempData["AvatarError"] = "Use a JPG, PNG or WebP under 2 MB.";
                return RedirectToAction(nameof(Edit));
            }

            var folder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(folder);

            // Never trust the client's filename; generate our own
            var fileName = $"{user.Id}_{Guid.NewGuid():N}{ext}";
            await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
            {
                await avatarFile.CopyToAsync(stream);
            }

            DeleteAvatarFile(user.ProfilePictureUrl); // remove the old one

            user.ProfilePictureUrl = $"/uploads/avatars/{fileName}";
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAvatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            DeleteAvatarFile(user.ProfilePictureUrl);

            user.ProfilePictureUrl = null; // falls back to the default avatar
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Button is hidden for verified users, but the URL can still be posted to directly
            if (await _userManager.IsEmailConfirmedAsync(user))
                return RedirectToAction(nameof(Index));

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = token },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                user.Email!,
                "GamingPlatform - Confirm Your Email",
                $@"Hello {user.UserName},<br/><br/>
                Please confirm your email by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>.");

            TempData["Success"] = $"Confirmation email sent to {user.Email}.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Other people's profiles ----------

        public async Task<IActionResult> Details(string requestId)
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            if (loggedInUser == null) return NotFound();

            var user = _userManager.Users
                .FirstOrDefault(u => u.UserName == requestId);
            if (user == null) return NotFound();

            // Viewing yourself -> go to your own profile page
            if (user.Id == loggedInUser.Id)
                return RedirectToAction(nameof(Index));

            // A block in either direction hides the profile
            if (_friendService.IsBlockedEitherWay(loggedInUser.Id, user.Id))
            {
                TempData["Blocked"] = "This user is unavailable.";
                return RedirectToAction(nameof(Index));
            }

            var friends = _friendService.GetFriends(user.Id);
            var games = _libraryService.GetUserGames(user.Id);

            var pendingRow = _friendService.GetPendingBetween(loggedInUser.Id, user.Id);
            ViewBag.CanViewWishlist = _friendService.CanViewWishlist(user, loggedInUser.Id);
            ViewBag.Friends = friends;
            ViewBag.IsFriend = friends.Any(f => f.Id == loggedInUser.Id);

            // I already sent them a request
            ViewBag.RequestSent = pendingRow != null && pendingRow.UserId == loggedInUser.Id;

            // They sent me one: the view offers Accept / Decline using this row id
            ViewBag.IncomingRequestId = pendingRow != null && pendingRow.FriendId == loggedInUser.Id
                ? pendingRow.Id
                : (Guid?)null;

            ViewBag.Games = BuildGameDtos(games, loggedInUser.Id);

            return View(user);
        }

        // ---------- Helpers ----------

        // wishlist/library flags are computed for the VIEWER
        private List<GameDto> BuildGameDtos(IEnumerable<Game> games, string viewerId)
        {
            return games.Select(g => new GameDto
            {
                Id = g.Id,
                Title = g.Title ?? "Unknown",
                Description = g.Description,
                GameGenres = g.GameGenres,
                Platform = g.Platform,
                Price = g.Price,
                CoverImageUrl = g.CoverImageUrl,
                SystemRequirements = g.SystemRequirements,
                Developers = g.GameDevelopers?
                    .Where(d => d.Developer != null)
                    .Select(d => new DevDto
                    {
                        Id = d.Developer.Id,
                        Name = d.Developer.StudioName ?? "Unknown"
                    })
                    .ToList() ?? new List<DevDto>(),
                Achievements = g.Achievements?
                    .Select(a => new AchievementDto
                    {
                        Id = a.Id,
                        Name = a.Name ?? "Unknown",
                        Description = a.Description
                    })
                    .ToList() ?? new List<AchievementDto>(),
                Rating = g.Reviews != null && g.Reviews.Any() ? g.Reviews.Average(r => r.Score) : (double?)null,
                InWishlist = _wishlistService.IsGameInWishlist(viewerId, g.Id),
                InLibrary = _libraryService.IsGameInLibrary(viewerId, g.Id)
            }).ToList();
        }

        private void DeleteAvatarFile(string? url)
        {
            // Only ever delete files inside our own avatars folder
            if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/avatars/")) return;

            var path = Path.Combine(_env.WebRootPath, url.TrimStart('/'));
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }
}