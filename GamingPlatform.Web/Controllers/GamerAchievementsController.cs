using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;

namespace GamingPlatform.Web.Controllers
{
    public class GamerAchievementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GamerAchievementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GamerAchievements
        public async Task<IActionResult> Index()
        {
            return View(await _context.GamerAchievements.ToListAsync());
        }

        // GET: GamerAchievements/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gamerAchievement = await _context.GamerAchievements
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gamerAchievement == null)
            {
                return NotFound();
            }

            return View(gamerAchievement);
        }

        // GET: GamerAchievements/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: GamerAchievements/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GamerId,AchievementId,DateUnlocked,Id")] GamerAchievement gamerAchievement)
        {
            if (ModelState.IsValid)
            {
                gamerAchievement.Id = Guid.NewGuid();
                _context.Add(gamerAchievement);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(gamerAchievement);
        }

        // GET: GamerAchievements/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gamerAchievement = await _context.GamerAchievements.FindAsync(id);
            if (gamerAchievement == null)
            {
                return NotFound();
            }
            return View(gamerAchievement);
        }

        // POST: GamerAchievements/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("GamerId,AchievementId,DateUnlocked,Id")] GamerAchievement gamerAchievement)
        {
            if (id != gamerAchievement.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gamerAchievement);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GamerAchievementExists(gamerAchievement.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(gamerAchievement);
        }

        // GET: GamerAchievements/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gamerAchievement = await _context.GamerAchievements
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gamerAchievement == null)
            {
                return NotFound();
            }

            return View(gamerAchievement);
        }

        // POST: GamerAchievements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var gamerAchievement = await _context.GamerAchievements.FindAsync(id);
            if (gamerAchievement != null)
            {
                _context.GamerAchievements.Remove(gamerAchievement);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GamerAchievementExists(Guid id)
        {
            return _context.GamerAchievements.Any(e => e.Id == id);
        }
    }
}
