using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;

namespace GamingPlatform.Web
{
    public class DevsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DevsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Devs
        public async Task<IActionResult> Index()
        {
            return View(await _context.Devs.ToListAsync());
        }

        // GET: Devs/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dev = await _context.Devs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dev == null)
            {
                return NotFound();
            }

            return View(dev);
        }

        // GET: Devs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Devs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Description,ProfilePicture,DateJoined,Email,StudioName,Id")] Dev dev)
        {
            if (ModelState.IsValid)
            {
                dev.Id = Guid.NewGuid();
                _context.Add(dev);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(dev);
        }

        // GET: Devs/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dev = await _context.Devs.FindAsync(id);
            if (dev == null)
            {
                return NotFound();
            }
            return View(dev);
        }

        // POST: Devs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Username,Description,ProfilePicture,DateJoined,Email,StudioName,Id")] Dev dev)
        {
            if (id != dev.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dev);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DevExists(dev.Id))
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
            return View(dev);
        }

        // GET: Devs/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dev = await _context.Devs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dev == null)
            {
                return NotFound();
            }

            return View(dev);
        }

        // POST: Devs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var dev = await _context.Devs.FindAsync(id);
            if (dev != null)
            {
                _context.Devs.Remove(dev);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DevExists(Guid id)
        {
            return _context.Devs.Any(e => e.Id == id);
        }
    }
}
