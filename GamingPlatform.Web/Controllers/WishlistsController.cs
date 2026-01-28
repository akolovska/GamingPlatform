using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Implementations;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
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
    public class WishlistsController : Controller
    {
        private readonly IWishlistService _wishlistService;
        private readonly ILibraryService _libraryService;
        public WishlistsController(IWishlistService wishlistService, ILibraryService libraryService)
        {
            _wishlistService = wishlistService;
            _libraryService = libraryService;
        }

        [HttpPost]
        public IActionResult AddToWishlist(Guid gameId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Add the game to the user's wishlist
            _wishlistService.AddGameToWishlist(userId, gameId);

            // Redirect back to the game details page
            return RedirectToAction(nameof(Index));
        }

        // GET: Wishlists
        [Authorize]
        public IActionResult Index()
        {
            var gamerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var games = _wishlistService.GetAllGamesFromWishlist(gamerId);
            var gameDtos = games.Select(g => new GameDto
            {
                Id = g.Id,
                Title = g.Title,
                Description = g.Description,
                Genre = g.Genre,
                Platform = g.Platform,
                Price = g.Price,
                CoverImageUrl = g.CoverImageUrl,
                SystemRequirements = g.SystemRequirements,
                Developers = g.GameDevelopers?
                         .Select(d => new DevDto { Id = d.DevId, Name = d.Developer?.StudioName ?? "Unknown" })
                         .ToList() ?? new List<DevDto>(),
                Achievements = g.Achievements?
                        .Select(a => new AchievementDto { Id = a.Id, Name = a.Name, Description = a.Description })
                        .ToList() ?? new List<AchievementDto>(),
                Rating = g.Reviews?.Any() == true ? g.Reviews.Average(r => r.Score) : (double?)null,
                InWishlist = _wishlistService.IsGameInWishlist(gamerId, g.Id),
                InLibrary = _libraryService.IsGameInLibrary(gamerId, g.Id)
            }).ToList();

            return View(gameDtos);
        }


        // GET: Wishlists/Details/5
        //public async Task<IActionResult> Details(Guid? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var wishlist = await _context.Wishlists
        //        .FirstOrDefaultAsync(m => m.Id == id);
        //    if (wishlist == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(wishlist);
        //}

        //// GET: Wishlists/Create
        //public IActionResult Create()
        //{
        //    return View();
        //}

        // POST: Wishlists/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("GamerId,GameId,Id")] Wishlist wishlist)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        wishlist.Id = Guid.NewGuid();
        //        _context.Add(wishlist);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(wishlist);
        //}

        // GET: Wishlists/Edit/5
        //public async Task<IActionResult> Edit(Guid? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var wishlist = await _context.Wishlists.FindAsync(id);
        //    if (wishlist == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(wishlist);
        //}

        //// POST: Wishlists/Edit/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(Guid id, [Bind("GamerId,GameId,Id")] Wishlist wishlist)
        //{
        //    if (id != wishlist.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(wishlist);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!WishlistExists(wishlist.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(wishlist);
        //}

        //// GET: Wishlists/Delete/5
        //public async Task<IActionResult> Delete(Guid? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var wishlist = await _context.Wishlists
        //        .FirstOrDefaultAsync(m => m.Id == id);
        //    if (wishlist == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(wishlist);
        //}

        //// POST: Wishlists/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(Guid id)
        //{
        //    var wishlist = await _context.Wishlists.FindAsync(id);
        //    if (wishlist != null)
        //    {
        //        _context.Wishlists.Remove(wishlist);
        //    }

        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(Index));
        //}

        //private bool WishlistExists(Guid id)
        //{
        //    return _context.Wishlists.Any(e => e.Id == id);
        //}
    }
}
