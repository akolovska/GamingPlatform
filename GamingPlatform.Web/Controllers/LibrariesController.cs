using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Implementations;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GamingPlatform.Web.Controllers
{
    public class LibrariesController : Controller
    {
        private readonly ILibraryService _libraryService;
        private readonly IGameService _gameService;
        private readonly IWishlistService _wishlistService;
        private readonly UserManager<GamingPlatformUser> _userManager;

        public LibrariesController(ILibraryService libraryService, IGameService gameService, IWishlistService wishlistService, UserManager<GamingPlatformUser> userManager)
        {
            _libraryService = libraryService;
            _gameService = gameService;
            _wishlistService = wishlistService;
            _userManager = userManager;
        }


        // GET: Libraries
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var games = _libraryService.GetUserGames(userId); // List<Game>

            // Map to GameDto
            var gameDtos = games.Select(g => new GameDto
            {
                Id = g.Id,
                Title = g.Title ?? "Unknown",
                Description = g.Description,
                Genre = g.Genre,
                Platform = g.Platform,
                Price = g.Price,
                CoverImageUrl = g.CoverImageUrl,
                SystemRequirements = g.SystemRequirements,
                Developers = g.GameDevelopers?
                    .Where(d => d.Developer != null)
                    .Select(d => new DevDto
                    {
                        Id = d.Developer.Id,
                        Name = d.Developer.StudioName ?? "Unknown"
                    })
                    .ToList() ?? new List<DevDto>(),
                Achievements = g.Achievements?
                    .Select(a => new AchievementDto
                    {
                        Id = a.Id,
                        Name = a.Name ?? "Unknown",
                        Description = a.Description
                    })
                    .ToList() ?? new List<AchievementDto>(),
                Rating = g.Reviews != null && g.Reviews.Any() ? g.Reviews.Average(r => r.Score) : (double?)null,
                InWishlist = _wishlistService.IsGameInWishlist(userId, g.Id),
                InLibrary = _libraryService.IsGameInLibrary(userId, g.Id)
            }).ToList();

            return View(gameDtos);
        }

        // GET: Libraries/Details/5
        public IActionResult Details(string id)
        {
            if (id == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var libraryEntry = _libraryService.GetGameFromLibrary(userId, Guid.Parse(id)); // returns Game object
            if (libraryEntry == null) return NotFound();

            // Map to GameDto
            var gameDto = new GameDto
            {
                Id = libraryEntry.Id,
                Title = libraryEntry.Title ?? "Unknown",
                Description = libraryEntry.Description,
                Genre = libraryEntry.Genre,
                Platform = libraryEntry.Platform,
                Price = libraryEntry.Price,
                CoverImageUrl = libraryEntry.CoverImageUrl,
                SystemRequirements = libraryEntry.SystemRequirements,
                // Map developers to DevDto
                Developers = libraryEntry.GameDevelopers?
                    .Where(d => d.Developer != null)
                    .Select(d => new DevDto
                    {
                        Id = d.Developer.Id,
                        Name = d.Developer.StudioName ?? "Unknown"
                    })
                    .ToList() ?? new List<DevDto>(),
                Achievements = libraryEntry.Achievements?
                    .Select(a => new AchievementDto
                    {
                        Id = a.Id,
                        Name = a.Name ?? "Unknown",
                        Description = a.Description
                    })
                    .ToList() ?? new List<AchievementDto>(),

                Rating = libraryEntry.Reviews != null && libraryEntry.Reviews.Any() ? libraryEntry.Reviews.Average(r => r.Score) : (double?)null,
                InWishlist = _wishlistService.IsGameInWishlist(userId, Guid.Parse(id)),
                InLibrary = _libraryService.IsGameInLibrary(userId, Guid.Parse(id))
            };

            return View(gameDto);
        }
        [HttpPost]
        public async Task<IActionResult> BuyGame(string stripeSessionId, Guid gameId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var user = await _userManager.FindByIdAsync(userId);
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Verified"] = "Please verify your email to access all features.";
                return RedirectToAction("Details", "Games");
            }

            var game = _gameService.GetGameById(gameId);
            if (game == null) return NotFound();

            // Create Stripe Checkout session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)((game.Price ?? 0) * 100), // cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = game.Title,
                            Description = game.Description
                        }
                    },
                    Quantity = 1
                }
            },
                Mode = "payment",
                SuccessUrl = Url.Action("PaymentSuccess", "Libraries", new { gameId = game.Id }, Request.Scheme),
                CancelUrl = Url.Action("Details", "Games", new { id = game.Id }, Request.Scheme)
            };
            StripeConfiguration.ApiKey = "sk_test_51RQSuVGamyUtSVoKBmOD9i5BRLIMh6MKrbQ6VI1yA6YOzlDkoAgkI1YaEZUKd8qkm4HiOD6oD4N2BYPh7P0ApxvW00SgfHUDnJ";
            var service = new SessionService();
            Session session = service.Create(options);

            // Redirect user to Stripe Checkout
            return Redirect(session.Url);
        }

        [HttpGet]
        public IActionResult PaymentSuccess(Guid gameId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Add the purchased game to library
            _libraryService.AddGameToLibrary(userId, gameId);

            TempData["Success"] = "Payment successful! Game added to your library.";
            return RedirectToAction(nameof(Index));
        }
    }
}


   
