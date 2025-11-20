using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Web.Controllers
{
    public class GamersController : Controller
    {
        private readonly IGamerService _gamerService;

        public GamersController(IGamerService gamerService)
        {
            _gamerService = gamerService;
        }

        // GET: Gamers
        public IActionResult Index()
        {
            return View(_gamerService.GetAllGamers());
        }

        // GET: Gamers/Details/5
        public IActionResult Details(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gamer = _gamerService.GetGamerById(id);
            if (gamer == null)
            {
                return NotFound();
            }

            return View(gamer);
        }

        // GET: Gamers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Gamers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Username,Description,ProfilePicture,DateJoined,Email,Id")] Gamer gamer)
        {
            if (ModelState.IsValid)
            {
                gamer.Id = Guid.NewGuid();
               _gamerService.AddGamer(gamer);
                return RedirectToAction(nameof(Index));
            }
            return View(gamer);
        }

        // GET: Gamers/Edit/5
        public IActionResult Edit(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gamer = _gamerService.GetGamerById(id);
            if (gamer == null)
            {
                return NotFound();
            }
            return View(gamer);
        }

        // POST: Gamers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, [Bind("Username,Description,ProfilePicture,DateJoined,Email,Id")] Gamer gamer)
        {
            if (id != gamer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _gamerService.UpdateGamer(gamer);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GamerExists(gamer.Id))
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
            return View(gamer);
        }

        // GET: Gamers/Delete/5
        public IActionResult Delete(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gamer = _gamerService.GetGamerById(id);
            if (gamer == null)
            {
                return NotFound();
            }

            return View(gamer);
        }

        // POST: Gamers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(Guid id)
        {
            var gamer = _gamerService.GetGamerById(id);
            if (gamer != null)
            {
                _gamerService.DeleteGamer(id);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool GamerExists(Guid id)
        {
            return _gamerService.GetAllGamers().Any(e => e.Id == id);
        }
    }
}
