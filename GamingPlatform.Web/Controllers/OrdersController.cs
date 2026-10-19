using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using GamingPlatform.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    // Buying: one game, a gift, or the cart. Paid orders go to Stripe Checkout and come back to Success.
    [Authorize]
    public class OrdersController : AppController
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // Purchase history
        public IActionResult Index() => View(_orderService.GetOrders(UserId!));

        [HttpPost, ValidateAntiForgeryToken, RequireVerifiedEmail]
        public async Task<IActionResult> BuyNow(Guid gameId) =>
            Checkout(await _orderService.BuyNowAsync(UserId!, gameId, SuccessUrl(), CancelUrl(gameId)), gameId);

        [HttpPost, ValidateAntiForgeryToken, RequireVerifiedEmail]
        public async Task<IActionResult> Gift(Guid gameId, string friendId) =>
            Checkout(await _orderService.GiftAsync(UserId!, gameId, friendId, SuccessUrl(), CancelUrl(gameId)), gameId);

        [HttpPost, ValidateAntiForgeryToken, RequireVerifiedEmail]
        public async Task<IActionResult> CheckoutCart() =>
            Checkout(await _orderService.CheckoutCartAsync(UserId!, SuccessUrl(), Url.Action("Index", "Cart", null, Request.Scheme)!), gameId: null);

        // Stripe sends the buyer back here after paying
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrEmpty(session_id)) return BadRequest();

            var result = await _orderService.CompleteCheckoutAsync(UserId!, session_id);
            if (result.Status is ResultStatus.NotFound or ResultStatus.Forbidden) return Failure(result);

            Flash(result);
            return RedirectToAction("Index", "Libraries");
        }

        private IActionResult Checkout(ServiceResult<string?> result, Guid? gameId)
        {
            if (result.Status is ResultStatus.NotFound or ResultStatus.Forbidden) return Failure(result);

            // Paid: off to Stripe Checkout
            if (result.Value != null) return Redirect(result.Value);

            Flash(result);
            if (result.Succeeded) return RedirectToAction("Index", "Libraries");
            return gameId == null
                ? RedirectToAction("Index", "Cart")
                : RedirectToAction("Details", "Games", new { id = gameId });
        }

        // Stripe replaces {CHECKOUT_SESSION_ID} with the real id; Url.Action would encode the braces
        private string SuccessUrl() =>
            Url.Action(nameof(Success), "Orders", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}";

        private string CancelUrl(Guid gameId) =>
            Url.Action("Details", "Games", new { id = gameId }, Request.Scheme)!;
    }
}
