using GamingPlatform.Domain.DTO;

namespace GamingPlatform.Service.Interfaces
{
    // Buying games: one game, a gift, or the whole cart. Each checkout creates an Order.
    // Free orders are completed at once (Value = null); paid ones return the Stripe Checkout URL.
    public interface IOrderService
    {
        Task<ServiceResult<string?>> BuyNowAsync(string buyerId, Guid gameId, string successUrl, string cancelUrl);
        Task<ServiceResult<string?>> GiftAsync(string buyerId, Guid gameId, string friendId, string successUrl, string cancelUrl);
        Task<ServiceResult<string?>> CheckoutCartAsync(string buyerId, string successUrl, string cancelUrl);

        // The buyer comes back from Stripe: fulfils the order if Stripe says it's paid
        Task<ServiceResult> CompleteCheckoutAsync(string buyerId, string sessionId);

        // POST /stripe/webhook. Handles paid, expired and refunded checkouts.
        // NotFound = webhook not configured, Invalid = bad signature, Ok = handled or ignored.
        ServiceResult HandleStripeWebhook(string json, string? signature);

        // Paid and refunded orders, newest first
        List<OrderDto> GetOrders(string buyerId);
    }
}
