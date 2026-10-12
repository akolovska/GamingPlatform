using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamingPlatform.Web.Controllers
{
    public class AchievementsController : Controller
    {
        private readonly IAchievementService _achievementService;

        public AchievementsController(IAchievementService achievementService)
        {
            _achievementService = achievementService;
        }

        // POST: Achievements/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin)]
        public async Task<IActionResult> Create([Bind("GameId,Name,Description,Id")] AchievementDto achievement)
        {
            if (ModelState.IsValid)
            {
                achievement.Id = Guid.NewGuid();
                _achievementService.AddAchievement(AchievementDto.ToAchievement(achievement));
            }
            return RedirectToAction("Edit", "Games", new { id = achievement.GameId });
        }

        // GET: Achievements/Edit/5
        [Authorize(Roles = Roles.DevOrAdmin)]
        public IActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var achievement = _achievementService.GetAchievementById(id.Value);
            if (achievement == null)
            {
                return NotFound();
            }
            var dto = AchievementDto.FromAchievement(achievement);
            return View(dto);
        }

        // POST: Achievements/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin)]
        public async Task<IActionResult> Edit(Guid id, [Bind("GameId,Name,Description,Id")] AchievementDto dto)
        {
            if (id != dto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _achievementService.UpdateAchievement(AchievementDto.ToAchievement(dto));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AchievementExists(dto.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return RedirectToAction("Edit", "Games", new { id = dto.GameId });
        }

        // GET: Achievements/Delete/5
        [Authorize(Roles = Roles.DevOrAdmin)]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var achievement = _achievementService.GetAchievementById(id.Value);
            if (achievement == null)
            {
                return NotFound();
            }

            return View(achievement);
        }

        // POST: Achievements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.DevOrAdmin)]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var achievement = _achievementService.GetAchievementById(id);
            if (achievement == null)
            {
                return NotFound();
            }
            _achievementService.DeleteAchievement(id);

            return RedirectToAction("Edit", "Games", new { id = achievement.GameId });
        }

        private bool AchievementExists(Guid id)
        {
            return _achievementService.GetAchievementById(id) != null;
        }
    }
}
