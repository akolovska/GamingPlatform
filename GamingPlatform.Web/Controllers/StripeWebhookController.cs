using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    // Stripe calls this when a checkout is paid or expires, and when a payment is refunded.
    // Register https://<your-site>/stripe/webhook in the Stripe dashboard for the events
    // checkout.session.completed, checkout.session.expired and charge.refunded,
    // and put its signing secret in .env as Stripe__WebhookSecret.
    [AllowAnonymous]
    public class StripeWebhookController : AppController
    {
        private readonly IOrderService _orderService;

        public StripeWebhookController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // No antiforgery token: Stripe isn't a browser form. The Stripe-Signature header is checked instead.
        [HttpPost("stripe/webhook"), IgnoreAntiforgeryToken]
        public async Task<IActionResult> Receive()
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync(); // the raw body: the signature is computed over it

            var result = _orderService.HandleStripeWebhook(json, Request.Headers["Stripe-Signature"]);
            return result.Status switch
            {
                ResultStatus.Ok => Ok(),
                ResultStatus.NotFound => NotFound(), // webhook not configured
                _ => BadRequest()                    // bad or missing signature
            };
        }
    }
}
