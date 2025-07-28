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
    public class GamerFriendsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GamerFriendsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GamerFriends
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.GamerFriends.Include(g => g.Friend).Include(g => g.Gamer);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: GamerFriends/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gamerFriend = await _context.GamerFriends
                .Include(g => g.Friend)
                .Include(g => g.Gamer)
                .FirstOrDefaultAsync(m => m.GamerId == id);
            if (gamerFriend == null)
            {
                return NotFound();
            }

            return View(gamerFriend);
        }

        // GET: GamerFriends/Create
        public IActionResult Create()
        {
            ViewData["FriendId"] = new SelectList(_context.Gamers, "Id", "Id");
            ViewData["GamerId"] = new SelectList(_context.Gamers, "Id", "Id");
            return View();
        }

        // POST: GamerFriends/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GamerId,FriendId,AddedOn,Id")] GamerFriend gamerFriend)
        {
            if (ModelState.IsValid)
            {
                _context.Add(gamerFriend);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["FriendId"] = new SelectList(_context.Gamers, "Id", "Id", gamerFriend.FriendId);
            ViewData["GamerId"] = new SelectList(_context.Gamers, "Id", "Id", gamerFriend.GamerId);
            return View(gamerFriend);
        }

        // GET: GamerFriends/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gamerFriend = await _context.GamerFriends.FindAsync(id);
            if (gamerFriend == null)
            {
                return NotFound();
            }
            ViewData["FriendId"] = new SelectList(_context.Gamers, "Id", "Id", gamerFriend.FriendId);
            ViewData["GamerId"] = new SelectList(_context.Gamers, "Id", "Id", gamerFriend.GamerId);
            return View(gamerFriend);
        }

        // POST: GamerFriends/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid? id, [Bind("GamerId,FriendId,AddedOn,Id")] GamerFriend gamerFriend)
        {
            if (id != gamerFriend.GamerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gamerFriend);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GamerFriendExists(gamerFriend.GamerId))
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
            ViewData["FriendId"] = new SelectList(_context.Gamers, "Id", "Id", gamerFriend.FriendId);
            ViewData["GamerId"] = new SelectList(_context.Gamers, "Id", "Id", gamerFriend.GamerId);
            return View(gamerFriend);
        }

        // GET: GamerFriends/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gamerFriend = await _context.GamerFriends
                .Include(g => g.Friend)
                .Include(g => g.Gamer)
                .FirstOrDefaultAsync(m => m.GamerId == id);
            if (gamerFriend == null)
            {
                return NotFound();
            }

            return View(gamerFriend);
        }

        // POST: GamerFriends/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid? id)
        {
            var gamerFriend = await _context.GamerFriends.FindAsync(id);
            if (gamerFriend != null)
            {
                _context.GamerFriends.Remove(gamerFriend);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GamerFriendExists(Guid? id)
        {
            return _context.GamerFriends.Any(e => e.GamerId == id);
        }
    }
}
