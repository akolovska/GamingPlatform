using GamingPlatform.Domain.Identity;
using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    // Genres and studios. Linking a user to a studio is how someone becomes a Dev.
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : AppController
    {
        private readonly IGenreService _genreService;
        private readonly IDevService _devService;
        private readonly IUserAdminService _userAdmin;

        public AdminController(IGenreService genreService, IDevService devService, IUserAdminService userAdmin)
        {
            _genreService = genreService;
            _devService = devService;
            _userAdmin = userAdmin;
        }

        // ---------- Users ----------

        public async Task<IActionResult> Users(string? q, int page = 1)
        {
            ViewBag.Query = q;
            return View(await _userAdmin.SearchAsync(q, page, UserId!));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(string id, string? q, int page = 1) =>
            BackToUsers(await _userAdmin.BanAsync(id, UserId!), q, page);

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanUser(string id, string? q, int page = 1) =>
            BackToUsers(await _userAdmin.UnbanAsync(id), q, page);

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAdmin(string id, bool makeAdmin, string? q, int page = 1) =>
            BackToUsers(await _userAdmin.SetAdminAsync(id, makeAdmin, UserId!), q, page);

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id, string? q, int page = 1) =>
            BackToUsers(await _userAdmin.DeleteAsync(id, UserId!), q, page);

        private IActionResult BackToUsers(ServiceResult result, string? q, int page)
        {
            if (result.Status == ResultStatus.NotFound) return Failure(result);
            Flash(result);
            return RedirectToAction(nameof(Users), new { q, page });
        }

        // ---------- Genres and studios ----------

        public IActionResult Index()
        {
            ViewBag.Genres = _genreService.GetAll();
            ViewBag.Studios = _devService.GetAll();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AddGenre(string name) => Done(_genreService.Add(name));

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteGenre(Guid id) => Done(_genreService.Delete(id));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudio(string name, string? ownerUserName) =>
            Done(await _devService.CreateAsync(name, ownerUserName));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStudioOwner(Guid id, string? ownerUserName) =>
            Done(await _devService.SetOwnerAsync(id, ownerUserName));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudio(Guid id) =>
            Done(await _devService.DeleteAsync(id));

        private IActionResult Done(ServiceResult result)
        {
            if (result.Status == ResultStatus.NotFound) return Failure(result);
            Flash(result);
            return RedirectToAction(nameof(Index));
        }
    }
}
