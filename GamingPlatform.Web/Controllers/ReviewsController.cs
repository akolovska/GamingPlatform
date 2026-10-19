using GamingPlatform.Domain.DTO;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    [Authorize]
    public class ReviewsController : AppController
    {
        private const string FormError = "Please give a score from 1 to 5 and a comment (max 1000 characters).";

        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(Guid gameId, ReviewFormDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = FormError;
                return RedirectToGame(gameId);
            }

            var result = _reviewService.Add(UserId!, gameId, dto);
            if (result.Status is Service.ResultStatus.NotFound or Service.ResultStatus.Forbidden) return Failure(result);

            Flash(result);
            return RedirectToGame(gameId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, ReviewFormDto dto, Guid gameId)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = FormError;
                return RedirectToGame(gameId);
            }

            var result = _reviewService.Update(id, UserId!, dto);
            if (!result.Succeeded) return Failure(result);

            Flash(result);
            return RedirectToGame(result.Value);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            var result = _reviewService.Delete(id, UserId!, IsAdmin);
            if (!result.Succeeded) return Failure(result);

            Flash(result);
            return RedirectToGame(result.Value);
        }

        // helpful = true for up, false for down. The same arrow again takes the vote back.
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Vote(Guid id, bool helpful, Guid gameId, string? reviews)
        {
            var result = _reviewService.Vote(id, UserId!, helpful);
            if (result.Status == Service.ResultStatus.NotFound) return Failure(result);
            if (!result.Succeeded) Flash(result);

            // Back to the review itself, keeping the page's review sorting
            return Redirect(Url.Action("Details", "Games", new { id = gameId, reviews }) + $"#review-{id}");
        }

        private IActionResult RedirectToGame(Guid gameId) =>
            RedirectToAction("Details", "Games", new { id = gameId });
    }
}
