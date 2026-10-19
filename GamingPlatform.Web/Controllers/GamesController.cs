using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using GamingPlatform.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GamingPlatform.Web.Controllers
{
    public class GamesController : AppController
    {
        private readonly IGameService _gameService;
        private readonly IGenreService _genreService;
        private readonly IDevService _devService;

        public GamesController(IGameService gameService, IGenreService genreService, IDevService devService)
        {
            _gameService = gameService;
            _genreService = genreService;
            _devService = devService;
        }

        // ---------- Public browsing ----------

        // ?q=...&genreId=...&free=true&onSale=true&comingSoon=true&sort=PriceLow&page=2
        [AllowAnonymous]
        public IActionResult Index(StoreQueryDto query)
        {
            ViewBag.Genres = _genreService.GetAll();
            ViewBag.Query = query;

            // The front page (no search, first page) also shows trending games and recommendations
            bool frontPage = !query.IsFiltered && query.Page <= 1;
            ViewBag.Trending = frontPage ? _gameService.GetTrending(UserId) : new List<GameDto>();
            ViewBag.Recommendations = frontPage && UserId != null
                ? _gameService.GetRecommendations(UserId)
                : new List<RecommendationDto>();

            return View(_gameService.Search(query, UserId));
        }

        // reviews = "helpful" (default) or "newest"
        [AllowAnonymous]
        public IActionResult Details(Guid id, string? reviews)
        {
            var game = _gameService.GetDetails(id, UserId, IsAdmin, reviews);
            if (game == null) return NotFound();

            ViewBag.ReviewSort = reviews == "newest" ? "newest" : "helpful";
            return View(game);
        }

        // ---------- Create (Dev or Admin) ----------

        [Authorize(Roles = Roles.DevOrAdmin), RequireVerifiedEmail]
        public IActionResult Create()
        {
            LoadFormOptions();
            return View(new GameFormDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin), RequireVerifiedEmail]
        public async Task<IActionResult> Create(GameFormDto form, IFormFile? coverFile)
        {
            if (ModelState.IsValid)
            {
                var result = await _gameService.CreateAsync(form, coverFile, UserId!, IsAdmin);
                if (result.Succeeded)
                {
                    Flash(result);
                    return RedirectToAction(nameof(Details), new { id = result.Value });
                }
                AddErrors(result);
            }

            LoadFormOptions();
            return View(form);
        }

        // ---------- Edit (the game's Dev, or Admin) ----------

        [Authorize(Roles = Roles.DevOrAdmin)]
        public IActionResult Edit(Guid id)
        {
            var edit = _gameService.GetForEdit(id, UserId!, IsAdmin);
            if (!edit.Succeeded) return Failure(edit);

            LoadEditData(edit.Value!);
            return View(edit.Value!.Form);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin), RequireVerifiedEmail]
        public async Task<IActionResult> Edit(Guid id, GameFormDto form, IFormFile? coverFile)
        {
            form.Id = id;
            if (ModelState.IsValid)
            {
                var result = await _gameService.UpdateAsync(id, form, coverFile, UserId!, IsAdmin);
                if (result.Succeeded)
                {
                    Flash(result);
                    return RedirectToAction(nameof(Details), new { id });
                }
                if (result.Status != ResultStatus.Invalid) return Failure(result);
                AddErrors(result);
            }

            // Re-show the page with the real cover and achievements, not posted values
            var edit = _gameService.GetForEdit(id, UserId!, IsAdmin);
            if (!edit.Succeeded) return Failure(edit);
            LoadEditData(edit.Value!);
            return View(form);
        }

        // ---------- Delist / relist (the game's Dev, or Admin) ----------

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin), RequireVerifiedEmail]
        public IActionResult SetListed(Guid id, bool listed)
        {
            var result = _gameService.SetListed(id, listed, UserId!, IsAdmin);
            if (!result.Succeeded) return Failure(result);

            Flash(result);
            return RedirectToAction(nameof(Details), new { id });
        }

        // ---------- Screenshots (the game's Dev, or Admin) ----------

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin), RequireVerifiedEmail]
        [RequestSizeLimit(25 * 1024 * 1024)] // up to 10 images of 2 MB, plus the form
        public async Task<IActionResult> AddScreenshots(Guid id, List<IFormFile> screenshots)
        {
            var result = await _gameService.AddScreenshotsAsync(id, screenshots, UserId!, IsAdmin);
            if (result.Status is ResultStatus.NotFound or ResultStatus.Forbidden) return Failure(result);

            Flash(result);
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin), RequireVerifiedEmail]
        public IActionResult DeleteScreenshot(Guid id, string url)
        {
            var result = _gameService.DeleteScreenshot(id, url, UserId!, IsAdmin);
            if (!result.Succeeded) return Failure(result);

            Flash(result);
            return RedirectToAction(nameof(Edit), new { id });
        }

        // ---------- Helpers ----------

        private void LoadFormOptions()
        {
            ViewBag.AllGenres = _genreService.GetAll();
            if (IsAdmin)
                ViewBag.Studios = new SelectList(_devService.GetAll(), nameof(StudioDto.Id), nameof(StudioDto.Name));
        }

        private void LoadEditData(GameEditDto edit)
        {
            ViewBag.AllGenres = _genreService.GetAll();
            ViewBag.CoverImageUrl = edit.CoverImageUrl;
            ViewBag.Achievements = edit.Achievements;
            ViewBag.Screenshots = edit.Screenshots;
        }
    }
}
