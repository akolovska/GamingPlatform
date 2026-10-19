using GamingPlatform.Service.Interfaces;
using GamingPlatform.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    [Authorize]
    public class WishlistsController : AppController
    {
        private readonly IWishlistService _wishlistService;

        public WishlistsController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        // My wishlist
        public IActionResult Index() => View(_wishlistService.GetMyWishlist(UserId!));

        // Someone else's wishlist; id is their user id
        public IActionResult Details(string id)
        {
            if (id == UserId) return RedirectToAction(nameof(Index));

            var result = _wishlistService.GetWishlist(id, UserId!);
            if (!result.Succeeded) return Failure(result);

            return result.Value!.IsVisible
                ? View(result.Value)
                : View("Private", result.Value.OwnerName);
        }

        [HttpPost, ValidateAntiForgeryToken, RequireVerifiedEmail]
        public IActionResult AddToWishlist(Guid gameId)
        {
            var result = _wishlistService.Add(UserId!, gameId);
            if (result.Status == Service.ResultStatus.NotFound) return Failure(result);

            Flash(result);
            return RedirectToAction("Details", "Games", new { id = gameId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RemoveFromWishlist(Guid gameId, string? returnTo)
        {
            Flash(_wishlistService.Remove(UserId!, gameId));

            // Removing from the wishlist page stays on the wishlist page
            return returnTo == "wishlist"
                ? RedirectToAction(nameof(Index))
                : RedirectToAction("Details", "Games", new { id = gameId });
        }
    }
}
