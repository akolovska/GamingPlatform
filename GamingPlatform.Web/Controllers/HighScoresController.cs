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
    public class HighScoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HighScoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: HighScores
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.HighScores.Include(h => h.Game).Include(h => h.Gamer);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: HighScores/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var highScore = await _context.HighScores
                .Include(h => h.Game)
                .Include(h => h.Gamer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (highScore == null)
            {
                return NotFound();
            }

            return View(highScore);
        }

        // GET: HighScores/Create
        public IActionResult Create()
        {
            ViewData["GameId"] = new SelectList(_context.Games, "Id", "Id");
            ViewData["GamerId"] = new SelectList(_context.Gamers, "Id", "Id");
            return View();
        }

        // POST: HighScores/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GamerId,GameId,Score,DateAchieved,Id")] HighScore highScore)
        {
            if (ModelState.IsValid)
            {
                highScore.Id = Guid.NewGuid();
                _context.Add(highScore);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GameId"] = new SelectList(_context.Games, "Id", "Id", highScore.GameId);
            ViewData["GamerId"] = new SelectList(_context.Gamers, "Id", "Id", highScore.GamerId);
            return View(highScore);
        }

        // GET: HighScores/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var highScore = await _context.HighScores.FindAsync(id);
            if (highScore == null)
            {
                return NotFound();
            }
            ViewData["GameId"] = new SelectList(_context.Games, "Id", "Id", highScore.GameId);
            ViewData["GamerId"] = new SelectList(_context.Gamers, "Id", "Id", highScore.GamerId);
            return View(highScore);
        }

        // POST: HighScores/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("GamerId,GameId,Score,DateAchieved,Id")] HighScore highScore)
        {
            if (id != highScore.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(highScore);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HighScoreExists(highScore.Id))
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
            ViewData["GameId"] = new SelectList(_context.Games, "Id", "Id", highScore.GameId);
            ViewData["GamerId"] = new SelectList(_context.Gamers, "Id", "Id", highScore.GamerId);
            return View(highScore);
        }

        // GET: HighScores/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var highScore = await _context.HighScores
                .Include(h => h.Game)
                .Include(h => h.Gamer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (highScore == null)
            {
                return NotFound();
            }

            return View(highScore);
        }

        // POST: HighScores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var highScore = await _context.HighScores.FindAsync(id);
            if (highScore != null)
            {
                _context.HighScores.Remove(highScore);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HighScoreExists(Guid id)
        {
            return _context.HighScores.Any(e => e.Id == id);
        }
    }
}
