using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GamingPlatform.Web.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly IGameService _gameService;
        private readonly ILibraryService _libraryService;

        public ReviewsController(IReviewService reviewService, IGameService gameService, ILibraryService libraryService)
        {
            _reviewService = reviewService;
            _gameService = gameService;
            _libraryService = libraryService;
        }

        // Only players who own the game, one review per game
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Guid gameId, ReviewFormDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (_gameService.GetGameById(gameId) == null) return NotFound();
            if (!_libraryService.IsGameInLibrary(userId, gameId)) return Forbid();

            if (_reviewService.GetUserReviewForGame(userId, gameId) != null)
            {
                TempData["Error"] = "You have already reviewed this game.";
                return RedirectToGame(gameId);
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please give a score from 1 to 5 and a comment.";
                return RedirectToGame(gameId);
            }

            _reviewService.AddReview(userId, gameId, dto);
            TempData["Success"] = "Review posted.";
            return RedirectToGame(gameId);
        }

        // Author, or Admin/Dev who manages the game
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, ReviewFormDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var review = _reviewService.GetReviewById(id);
            if (review?.GameId == null) return NotFound();

            var game = _gameService.GetGameById(review.GameId.Value);
            if (game == null) return NotFound();
            if (review.GamerId != userId && !CanModerate(game, userId)) return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please give a score from 1 to 5 and a comment.";
                return RedirectToGame(game.Id);
            }

            _reviewService.UpdateReview(review, dto);
            TempData["Success"] = "Review updated.";
            return RedirectToGame(game.Id);
        }

        // Admin, or Dev who manages the game
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var review = _reviewService.GetReviewById(id);
            if (review?.GameId == null) return NotFound();

            var game = _gameService.GetGameById(review.GameId.Value);
            if (game == null) return NotFound();
            if (!CanModerate(game, userId)) return Forbid();

            _reviewService.DeleteReview(review);
            TempData["Success"] = "Review deleted.";
            return RedirectToGame(game.Id);
        }

        // Same rule as GamesController.CanManage: Admin any game, Dev only their studio's games
        private bool CanModerate(Game game, string userId)
        {
            if (User.IsInRole(Roles.Admin)) return true;
            if (!User.IsInRole(Roles.Dev)) return false;

            return game.GameDevelopers?.Any(gd => gd.Developer?.UserId == userId) == true;
        }

        private IActionResult RedirectToGame(Guid gameId) =>
            RedirectToAction("Details", "Games", new { id = gameId });
    }
}
