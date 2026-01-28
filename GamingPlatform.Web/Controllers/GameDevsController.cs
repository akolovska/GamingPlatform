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
    public class GameDevsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GameDevsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GameDevs
        public async Task<IActionResult> Index()
        {
            return View(await _context.GameDevs.ToListAsync());
        }

        // GET: GameDevs/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameDev = await _context.GameDevs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gameDev == null)
            {
                return NotFound();
            }

            return View(gameDev);
        }

        // GET: GameDevs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: GameDevs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GameId,DevId,Id")] GameDev gameDev)
        {
            if (ModelState.IsValid)
            {
                gameDev.Id = Guid.NewGuid();
                _context.Add(gameDev);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(gameDev);
        }

        // GET: GameDevs/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameDev = await _context.GameDevs.FindAsync(id);
            if (gameDev == null)
            {
                return NotFound();
            }
            return View(gameDev);
        }

        // POST: GameDevs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("GameId,DevId,Id")] GameDev gameDev)
        {
            if (id != gameDev.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gameDev);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GameDevExists(gameDev.Id))
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
            return View(gameDev);
        }

        // GET: GameDevs/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameDev = await _context.GameDevs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gameDev == null)
            {
                return NotFound();
            }

            return View(gameDev);
        }

        // POST: GameDevs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var gameDev = await _context.GameDevs.FindAsync(id);
            if (gameDev != null)
            {
                _context.GameDevs.Remove(gameDev);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GameDevExists(Guid id)
        {
            return _context.GameDevs.Any(e => e.Id == id);
        }
    }
}
