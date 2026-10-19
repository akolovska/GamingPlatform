using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    [Authorize]
    public class CartController : AppController
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        public IActionResult Index() => View(_cartService.GetCart(UserId!));

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Add(Guid gameId)
        {
            var result = _cartService.Add(UserId!, gameId);
            if (result.Status == ResultStatus.NotFound) return Failure(result);

            Flash(result);
            return RedirectToAction("Details", "Games", new { id = gameId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Remove(Guid gameId)
        {
            Flash(_cartService.Remove(UserId!, gameId));
            return RedirectToAction(nameof(Index));
        }
    }
}
