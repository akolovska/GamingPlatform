using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GamingPlatform.Web.Controllers
{
    public class AchievementsController : Controller
    {
        private readonly IAchievementService _achievementService;
        private readonly IGameService _gameService;

        public AchievementsController(IAchievementService achievementService, IGameService gameService)
        {
            _achievementService = achievementService;
            _gameService = gameService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin)]
        public IActionResult Create(AchievementFormDto form, Guid gameId)
        {
            var game = _gameService.GetGameById(gameId);
            if (game == null) return NotFound();
            if (!CanManage(game)) return Forbid();

            if (ModelState.IsValid)
            {
                Achievement achievement = new Achievement
                {
                    GameId = gameId,
                    Name = form.Name,
                    Description = form.Description,
                    Id = Guid.NewGuid()
                };
                _achievementService.AddAchievement(achievement);
            }
            else
            {
                TempData["Error"] = "Achievement not added. Name is required (max 100 characters), description max 500.";
            }

            return RedirectToAction("Edit", "Games", new { id = gameId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin)]
        public IActionResult Edit(Guid id, AchievementFormDto form)
        {
            var achievement = _achievementService.GetAchievementById(id);
            if (achievement == null) return NotFound();
            if (!CanManageAchievement(achievement)) return Forbid();

            if (ModelState.IsValid)
            {
                achievement.Name = form.Name;
                achievement.Description = form.Description;
                _achievementService.UpdateAchievement(achievement);
            }
            else
            {
                TempData["Error"] = "Achievement not saved. Name is required (max 100 characters), description max 500.";
            }

            return RedirectToAction("Edit", "Games", new { id = achievement.GameId });
        }

        // POST: Achievements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin)]
        public IActionResult DeleteConfirmed(Guid id)
        {
            var achievement = _achievementService.GetAchievementById(id);
            if (achievement == null)
            {
                return NotFound();
            }
            if (!CanManageAchievement(achievement)) return Forbid();

            _achievementService.DeleteAchievement(id);

            return RedirectToAction("Edit", "Games", new { id = achievement.GameId });
        }

        // Same rule as GamesController.CanManage: Admin any game, Dev only games linked to their own studio.
        private bool CanManage(Game game)
        {
            if (User.IsInRole(Roles.Admin)) return true;
            if (!User.IsInRole(Roles.Dev)) return false;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return game.GameDevelopers?.Any(gd => gd.Developer?.UserId == userId) == true;
        }

        private bool CanManageAchievement(Achievement achievement)
        {
            var game = _gameService.GetGameById(achievement.GameId);
            return game != null && CanManage(game);
        }
    }
}
