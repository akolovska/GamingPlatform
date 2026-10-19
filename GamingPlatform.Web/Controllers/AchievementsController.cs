using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    // Achievements are edited from the game's Edit page
    [Authorize(Roles = Roles.DevOrAdmin)]
    public class AchievementsController : AppController
    {
        private const string FormError = "Name is required (max 100 characters), description max 500.";

        private readonly IAchievementService _achievementService;

        public AchievementsController(IAchievementService achievementService)
        {
            _achievementService = achievementService;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(Guid gameId, AchievementFormDto form)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = FormError;
                return RedirectToEdit(gameId);
            }

            var result = _achievementService.Add(gameId, form, UserId!, IsAdmin);
            if (!result.Succeeded) return Failure(result);

            Flash(result);
            return RedirectToEdit(gameId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, AchievementFormDto form, Guid gameId)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = FormError;
                return RedirectToEdit(gameId);
            }

            var result = _achievementService.Update(id, form, UserId!, IsAdmin);
            if (!result.Succeeded) return Failure(result);

            Flash(result);
            return RedirectToEdit(result.Value);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            var result = _achievementService.Delete(id, UserId!, IsAdmin);
            if (!result.Succeeded) return Failure(result);

            Flash(result);
            return RedirectToEdit(result.Value);
        }

        private IActionResult RedirectToEdit(Guid gameId) =>
            RedirectToAction("Edit", "Games", new { id = gameId });
    }
}
