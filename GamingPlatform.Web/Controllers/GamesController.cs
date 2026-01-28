using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Implementations;
using GamingPlatform.Service.Interfaces;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GamingPlatform.Web.Controllers
{
    public class GamesController : Controller
    {
        private readonly IGameService _gameService;
        private readonly ILibraryService _libraryService;
        private readonly IWishlistService _wishlistService;

        public GamesController(IGameService gameService, ILibraryService libraryService, IWishlistService wishlistService)
        {
            _gameService = gameService;
            _libraryService = libraryService;
            _wishlistService = wishlistService;
        }

        // GET: Games
        public IActionResult Index(string genre, string filter)
        {
            List<Game> games = _gameService.GetAllGames();
            if (!string.IsNullOrEmpty(genre))
                games = games.Where(x => x.Genre == genre).ToList();
            if (filter == "Free")
                games = games.Where(g => g.Price == 0).ToList();
            return View(games);
        }

         // GET: Games/Details/5
            public IActionResult Details(string id)
            {
                if (id == null)
                {
                    return NotFound();
                }

                var game = _gameService.GetGameById(Guid.Parse(id));
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var gameDto = new GameDto
            {
                Id = game.Id,
                Title = game.Title,
                Description = game.Description,
                Genre = game.Genre,
                Platform = game.Platform,
                Price = game.Price,
                CoverImageUrl = game.CoverImageUrl,
                SystemRequirements = game.SystemRequirements,
                Developers = game.GameDevelopers?
                    .Select(gd => new DevDto
                    {
                        Id = gd.DevId.Value,
                        Name = gd.Developer?.StudioName ?? "Unknown"
                    })
                    .ToList() ?? new List<DevDto>(),


                Achievements = game.Achievements?
                   .Select(a => new AchievementDto
                   {
                       Id = a.Id,
                       Name = a.Name,
                       Description = a.Description
                   }).ToList() ?? new List<AchievementDto>(),

                Reviews = game.Reviews?
                  .Select(r => new ReviewDto
                  {
                      Comment = r.Comment,
                      Score = r.Score,
                      DateRated = r.DateRated
                  }).ToList() ?? new List<ReviewDto>(),

                Rating = game.Reviews?.Any() == true ? game.Reviews.Average(r => r.Score) : null,
                InWishlist = _wishlistService.IsGameInWishlist(userId, Guid.Parse(id)),
                InLibrary = _libraryService.IsGameInLibrary(userId, Guid.Parse(id))
            };

            if (game == null)
                    {
                        return NotFound();
                    }

                    return View(gameDto);
           }

        //    // GET: Games/Create
        //    public IActionResult Create()
        //    {
        //        return View();
        //    }

        //    // POST: Games/Create
        //    // To protect from overposting attacks, enable the specific properties you want to bind to.
        //    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //    [HttpPost]
        //    [ValidateAntiForgeryToken]
        //    public async Task<IActionResult> Create([Bind("Title,Description,Genre,ReleaseDate,Price,Platform,CoverImageUrl,Id")] Game game)
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            game.Id = Guid.NewGuid();
        //            _context.Add(game);
        //            await _context.SaveChangesAsync();
        //            return RedirectToAction(nameof(Index));
        //        }
        //        return View(game);
        //    }

        //    // GET: Games/Edit/5
        //    public async Task<IActionResult> Edit(Guid? id)
        //    {
        //        if (id == null)
        //        {
        //            return NotFound();
        //        }

        //        var game = await _context.Games.FindAsync(id);
        //        if (game == null)
        //        {
        //            return NotFound();
        //        }
        //        return View(game);
        //    }

        //    // POST: Games/Edit/5
        //    // To protect from overposting attacks, enable the specific properties you want to bind to.
        //    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //    [HttpPost]
        //    [ValidateAntiForgeryToken]
        //    public async Task<IActionResult> Edit(Guid id, [Bind("Title,Description,Genre,ReleaseDate,Price,Platform,CoverImageUrl,Id")] Game game)
        //    {
        //        if (id != game.Id)
        //        {
        //            return NotFound();
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            try
        //            {
        //                _context.Update(game);
        //                await _context.SaveChangesAsync();
        //            }
        //            catch (DbUpdateConcurrencyException)
        //            {
        //                if (!GameExists(game.Id))
        //                {
        //                    return NotFound();
        //                }
        //                else
        //                {
        //                    throw;
        //                }
        //            }
        //            return RedirectToAction(nameof(Index));
        //        }
        //        return View(game);
        //    }

        //    // GET: Games/Delete/5
        //    public async Task<IActionResult> Delete(Guid? id)
        //    {
        //        if (id == null)
        //        {
        //            return NotFound();
        //        }

        //        var game = await _context.Games
        //            .FirstOrDefaultAsync(m => m.Id == id);
        //        if (game == null)
        //        {
        //            return NotFound();
        //        }

        //        return View(game);
        //    }

        //    // POST: Games/Delete/5
        //    [HttpPost, ActionName("Delete")]
        //    [ValidateAntiForgeryToken]
        //    public async Task<IActionResult> DeleteConfirmed(Guid id)
        //    {
        //        var game = await _context.Games.FindAsync(id);
        //        if (game != null)
        //        {
        //            _context.Games.Remove(game);
        //        }

        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }

        //    private bool GameExists(Guid id)
        //    {
        //        return _context.Games.Any(e => e.Id == id);
        //    }
    }
}
