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
    public class HighScoresController : Controller
    {
        private readonly IHighScoreService _highScoreService;
        private readonly IGameService _gameService;
        private readonly IGamerService _gamerService;

        public HighScoresController(IHighScoreService highScoreService, IGameService gameService, IGamerService gamerService)
        {
            _highScoreService = highScoreService;
            _gameService = gameService;
            _gamerService = gamerService;
        }

        // GET: HighScores
        public IActionResult Index()
        {
            return View(_highScoreService.GetAllHighScores());
        }

        // GET: HighScores/Details/5
        public IActionResult Details(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var highScore = _highScoreService.getHighScoreById(id);
            if (highScore == null)
            {
                return NotFound();
            }

            return View(highScore);
        }

        // GET: HighScores/Create
        public IActionResult Create()
        {
            ViewData["GameId"] = new SelectList(_gameService.GetAllGames(), "Id", "Id");
            ViewData["GamerId"] = new SelectList(_gamerService.GetAllGamers(), "Id", "Id");
            return View();
        }

        // POST: HighScores/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("GamerId,GameId,Score,DateAchieved,Id")] HighScore highScore)
        {
            if (ModelState.IsValid)
            {
                highScore.Id = Guid.NewGuid();
                _highScoreService.AddHighScore(highScore);
                return RedirectToAction(nameof(Index));
            }
            ViewData["GameId"] = new SelectList(_gameService.GetAllGames(), "Id", "Id");
            ViewData["GamerId"] = new SelectList(_gamerService.GetAllGamers(), "Id", "Id");
            return View(highScore);
        }

        // GET: HighScores/Edit/5
        public IActionResult Edit(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var highScore = _highScoreService.getHighScoreById(id);
            if (highScore == null)
            {
                return NotFound();
            }
            ViewData["GameId"] = new SelectList(_gameService.GetAllGames(), "Id", "Id");
            ViewData["GamerId"] = new SelectList(_gamerService.GetAllGamers(), "Id", "Id");
            return View(highScore);
        }

        // POST: HighScores/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, [Bind("GamerId,GameId,Score,DateAchieved,Id")] HighScore highScore)
        {
            if (id != highScore.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _highScoreService.UpdateHighScore(highScore);
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
            ViewData["GameId"] = new SelectList(_gameService.GetAllGames(), "Id", "Id");
            ViewData["GamerId"] = new SelectList(_gamerService.GetAllGamers(), "Id", "Id");
            return View(highScore);
        }

        // GET: HighScores/Delete/5
        public IActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var highScore = _highScoreService.getHighScoreById(id.Value);
            if (highScore == null)
            {
                return NotFound();
            }

            return View(highScore);
        }

        // POST: HighScores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(Guid id)
        {
            var highScore = _highScoreService.getHighScoreById(id);
            if (highScore != null)
            {
                _highScoreService.DeleteHighScore(id);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool HighScoreExists(Guid id)
        {
            return _highScoreService.getHighScoreById(id) != null;
        }
    }
}
