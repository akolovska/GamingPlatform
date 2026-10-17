using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace GamingPlatform.Web.Controllers
{
    public class GamesController : Controller
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxBytes = 2 * 1024 * 1024; // 2 MB
        private readonly IGameService _gameService;
        private readonly ILibraryService _libraryService;
        private readonly IWishlistService _wishlistService;
        private readonly IDevService _devService;
        private readonly IGenreService _genreService;
        private readonly UserManager<GamingPlatformUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public GamesController(
            IGameService gameService,
            ILibraryService libraryService,
            IWishlistService wishlistService,
            IDevService devService,
            IGenreService genreService,
            UserManager<GamingPlatformUser> userManager,
            IWebHostEnvironment env)
        {
            _gameService = gameService;
            _libraryService = libraryService;
            _wishlistService = wishlistService;
            _devService = devService;
            _genreService = genreService;
            _userManager = userManager;
            _env = env;
        }

        // ---------- Public browsing ----------

        [AllowAnonymous]
        public IActionResult Index(Guid? genreId, string? filter)
        {
            var games = _gameService.GetAllGames(); // delisted games excluded by default

            ViewBag.Genres = _genreService.GetAll();

            bool isFiltered = genreId.HasValue || !string.IsNullOrEmpty(filter);
            ViewBag.Trending = isFiltered ? new List<Game>() : _gameService.GetTrending();

            if (genreId.HasValue)
                games = games.Where(g => g.GameGenres.Any(x => x.GenreId == genreId)).ToList();
            if (filter == "Free")
                games = games.Where(g => g.Price == 0).ToList();

            ViewBag.SelectedGenreId = genreId;
            ViewBag.SelectedFilter = filter;
            return View(games);
        }

        [AllowAnonymous]
        public IActionResult Details(string id)
        {
            if (!Guid.TryParse(id, out var gameId)) return NotFound();

            var game = _gameService.GetGameById(gameId);
            if (game == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // null if not logged in

            // Delisted games are visible only to owners and to people who manage them
            bool isOwner = userId != null && _libraryService.IsGameInLibrary(userId, gameId);
            bool canManage = userId != null && CanManage(game, userId);
            if (game.IsDelisted && !isOwner && !canManage) return NotFound();

            // One query for all review authors; missing ids are deleted accounts
            var authorIds = game.Reviews?.Where(r => r.GamerId != null).Select(r => r.GamerId!).Distinct().ToList()
                            ?? new List<string>();
            var authorNames = _userManager.Users
                .Where(u => authorIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.UserName ?? "Unknown");

            var gameDto = new GameDto
            {
                Id = game.Id,
                Title = game.Title,
                Description = game.Description,
                GameGenres = game.GameGenres,
                Platform = game.Platform,
                Price = game.Price,
                CoverImageUrl = game.CoverImageUrl,
                SystemRequirements = game.SystemRequirements,
                Developers = game.GameDevelopers?
                    .Select(gd => new DevDto
                    {
                        Id = gd.DevId ?? Guid.Empty,
                        Name = gd.Developer?.StudioName ?? "Unknown"
                    }).ToList() ?? new List<DevDto>(),
                Achievements = game.Achievements?
                    .Select(a => new AchievementDto { Id = a.Id, Name = a.Name, Description = a.Description })
                    .ToList() ?? new List<AchievementDto>(),
                Reviews = game.Reviews?
                    .Select(r => new ReviewDto
                    {
                        Id = r.Id,
                        Comment = r.Comment,
                        Score = r.Score,
                        DateRated = r.DateRated,
                        IsMine = userId != null && r.GamerId == userId,
                        GamerName = authorNames.GetValueOrDefault(r.GamerId ?? "", "Deleted user")
                    })
                    .ToList() ?? new List<ReviewDto>(),
                Rating = game.Reviews?.Any() == true ? game.Reviews.Average(r => r.Score) : null,
                InWishlist = userId != null && _wishlistService.IsGameInWishlist(userId, gameId),
                InLibrary = isOwner
            };

            ViewBag.IsDelisted = game.IsDelisted;
            ViewBag.CanEdit = canManage;
            // Owners can write one review; after that they edit it instead
            ViewBag.CanReview = isOwner && game.Reviews?.Any(r => r.GamerId == userId) != true;

            return View(gameDto);
        }

        // ---------- Create (Dev or Admin) ----------

        [Authorize(Roles = Roles.DevOrAdmin)]
        public IActionResult Create()
        {
            LoadStudiosForAdmin();
            LoadGenres();
            return View(new GameFormDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin)]
        public async Task<IActionResult> Create(GameFormDto form)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!await EnsureVerifiedAsync(user)) return RedirectToAction("Index", "Account");

            Dev? studio = User.IsInRole(Roles.Admin)
                ? (form.DevId.HasValue ? _devService.GetDevById(form.DevId.Value) : null)
                : _devService.GetDevByUserId(user.Id);

            if (studio == null)
                ModelState.AddModelError("", User.IsInRole(Roles.Admin)
                    ? "Please choose a studio."
                    : "Your account is not linked to a studio. Contact an admin.");

            ValidateGenres(form);

            if (form.CoverFile == null || form.CoverFile.Length == 0)
                ModelState.AddModelError(nameof(form.CoverFile), "Please choose a cover image.");
            else
                ValidateCover(form.CoverFile);

            if (!ModelState.IsValid)
            {
                LoadStudiosForAdmin();
                LoadGenres();
                return View(form);
            }

            // Only save the file once everything else is valid
            form.CoverImageUrl = await SaveCoverAsync(form.CoverFile!);

            var game = new Game
            {
                Id = Guid.NewGuid(),
                Title = form.Title,
                Description = form.Description,
                ReleaseDate = form.ReleaseDate,
                Price = form.Price,
                Platform = form.Platform,
                CoverImageUrl = form.CoverImageUrl,
                SystemRequirements = form.SystemRequirements,
                GameDevelopers = new List<GameDev> { new GameDev { DevId = studio!.Id } },
                GameGenres = form.GenreIds.Select(id => new GameGenre { GenreId = id }).ToList()
            };

            _gameService.AddGame(game);
            return RedirectToAction(nameof(Details), new { id = game.Id });
        }

        // ---------- Edit (Dev who owns it, or Admin) ----------

        [Authorize(Roles = Roles.DevOrAdmin)]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var game = _gameService.GetGameById(id.Value);
            if (game == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!CanManage(game, user.Id)) return Forbid();

            LoadGenres();
            LoadAchievements(game);
            return View(new GameFormDto
            {
                Id = game.Id,
                Title = game.Title,
                Description = game.Description,
                ReleaseDate = game.ReleaseDate,
                Price = game.Price,
                Platform = game.Platform,
                CoverImageUrl = game.CoverImageUrl,
                SystemRequirements = game.SystemRequirements,
                GenreIds = game.GameGenres.Select(x => x.GenreId).ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin)]
        public async Task<IActionResult> Edit(Guid id, GameFormDto form)
        {
            if (id != form.Id) return NotFound();

            var game = _gameService.GetGameById(id);
            if (game == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!await EnsureVerifiedAsync(user)) return RedirectToAction("Index", "Account");
            if (!CanManage(game, user.Id)) return Forbid(); // re-check on POST

            ValidateGenres(form);

            // A new cover is optional on Edit; no file means keep the current one
            bool hasNewCover = form.CoverFile != null && form.CoverFile.Length > 0;
            if (hasNewCover)
                ValidateCover(form.CoverFile!);

            if (!ModelState.IsValid)
            {
                form.CoverImageUrl = game.CoverImageUrl; // re-show the real cover, not the posted hidden value
                LoadGenres();
                LoadAchievements(game);
                return View(form);
            }

            if (hasNewCover)
            {
                var oldCover = game.CoverImageUrl;
                game.CoverImageUrl = await SaveCoverAsync(form.CoverFile!);
                DeleteCoverFile(oldCover);
            }

            game.Title = form.Title;
            game.Description = form.Description;
            game.ReleaseDate = form.ReleaseDate;
            game.Price = form.Price;
            game.Platform = form.Platform;
            game.SystemRequirements = form.SystemRequirements;

            // Replace the genre links
            game.GameGenres.Clear();
            foreach (var genreId in form.GenreIds)
                game.GameGenres.Add(new GameGenre { GameId = game.Id, GenreId = genreId });

            _gameService.UpdateGame(game);
            return RedirectToAction(nameof(Details), new { id });
        }

        // ---------- Delist / Relist (Dev who owns it, or Admin) ----------

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin)]
        public async Task<IActionResult> SetListed(Guid id, bool listed)
        {
            var game = _gameService.GetGameById(id);
            if (game == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!await EnsureVerifiedAsync(user)) return RedirectToAction("Index", "Account");
            if (!CanManage(game, user.Id)) return Forbid();

            game.IsDelisted = !listed;
            _gameService.UpdateGame(game);

            TempData["GameMessage"] = listed
                ? "Game is listed in the store again."
                : "Game removed from the store. Owners keep it.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ---------- Helpers ----------

        // Admin: any game. Dev: only games linked to their own studio.
        private bool CanManage(Game game, string userId)
        {
            if (User.IsInRole(Roles.Admin)) return true;
            if (!User.IsInRole(Roles.Dev)) return false;

            return game.GameDevelopers?.Any(gd => gd.Developer?.UserId == userId) == true;
        }

        private async Task<bool> EnsureVerifiedAsync(GamingPlatformUser user)
        {
            if (await _userManager.IsEmailConfirmedAsync(user)) return true;

            TempData["Verified"] = "Please verify your email to access all features.";
            return false;
        }

        private void LoadStudiosForAdmin()
        {
            if (User.IsInRole(Roles.Admin))
                ViewBag.Studios = new SelectList(_devService.GetAllDevs(), "Id", "StudioName");
        }

        private void LoadGenres() => ViewBag.AllGenres = _genreService.GetAll();

        private void LoadAchievements(Game game) =>
            ViewBag.Achievements = game.Achievements?.ToList() ?? new List<Achievement>();

        // Drops forged ids, removes duplicates, requires at least one genre
        private void ValidateGenres(GameFormDto form)
        {
            var valid = _genreService.GetAll().Select(g => g.Id).ToHashSet();
            form.GenreIds = form.GenreIds.Where(valid.Contains).Distinct().ToList();

            if (!form.GenreIds.Any())
                ModelState.AddModelError(nameof(form.GenreIds), "Choose at least one genre.");
        }

        private void ValidateCover(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext) || file.Length > MaxBytes)
                ModelState.AddModelError(nameof(GameFormDto.CoverFile), "Use a JPG, PNG or WebP under 2 MB.");
        }

        // Saves to wwwroot/uploads/covers and returns the public URL
        private async Task<string> SaveCoverAsync(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "covers");
            Directory.CreateDirectory(folder);

            // Never trust the client's filename; generate our own
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/covers/{fileName}";
        }

        private void DeleteCoverFile(string? url)
        {
            // Only ever delete files inside our own covers folder (old covers may be external URLs)
            if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/covers/")) return;

            var path = Path.Combine(_env.WebRootPath, url.TrimStart('/'));
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }
}