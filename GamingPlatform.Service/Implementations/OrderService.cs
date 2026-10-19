using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GamingPlatform.Service.Implementations
{
    public class OrderService : IOrderService
    {
        // Stripe can't charge less than $0.50 in USD
        private const decimal StripeMinimumCharge = 0.50m;

        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Library> _libraryRepository;
        private readonly IRepository<Wishlist> _wishlistRepository;
        private readonly IRepository<CartItem> _cartRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGameService _gameService;
        private readonly IFriendService _friendService;
        private readonly INotificationService _notifications;
        private readonly UserManager<GamingPlatformUser> _userManager;
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IRepository<Order> orderRepository,
            IRepository<Library> libraryRepository,
            IRepository<Wishlist> wishlistRepository,
            IRepository<CartItem> cartRepository,
            IUnitOfWork unitOfWork,
            IGameService gameService,
            IFriendService friendService,
            INotificationService notifications,
            UserManager<GamingPlatformUser> userManager,
            IOptions<StripeSettings> stripeSettings,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _libraryRepository = libraryRepository;
            _wishlistRepository = wishlistRepository;
            _cartRepository = cartRepository;
            _unitOfWork = unitOfWork;
            _gameService = gameService;
            _friendService = friendService;
            _notifications = notifications;
            _userManager = userManager;
            _stripeSettings = stripeSettings.Value;
            _logger = logger;
        }

        // ---------- Checkout ----------

        public Task<ServiceResult<string?>> BuyNowAsync(string buyerId, Guid gameId, string successUrl, string cancelUrl) =>
            CheckoutAsync(buyerId, new List<(Guid, string)> { (gameId, buyerId) }, successUrl, cancelUrl);

        public Task<ServiceResult<string?>> GiftAsync(string buyerId, Guid gameId, string friendId, string successUrl, string cancelUrl)
        {
            // Only friends: a gift to a stranger is the classic way to spam or harass someone
            bool isFriend = friendId != buyerId && _friendService.GetFriends(buyerId).Any(f => f.Id == friendId);
            if (!isFriend)
                return Task.FromResult(ServiceResult<string?>.Invalid("You can only send gifts to your friends."));

            return CheckoutAsync(buyerId, new List<(Guid, string)> { (gameId, friendId) }, successUrl, cancelUrl);
        }

        public Task<ServiceResult<string?>> CheckoutCartAsync(string buyerId, string successUrl, string cancelUrl)
        {
            var gameIds = _cartRepository.GetAll(c => c.GameId, c => c.UserId == buyerId, q => q.OrderBy(c => c.AddedAt));
            if (gameIds.Count == 0)
                return Task.FromResult(ServiceResult<string?>.Invalid("Your cart is empty."));

            return CheckoutAsync(buyerId, gameIds.Select(id => (id, buyerId)).ToList(), successUrl, cancelUrl);
        }

        // Creates the order with today's prices. Free orders are fulfilled at once;
        // paid ones get a Stripe Checkout session and are fulfilled when Stripe says they're paid.
        private async Task<ServiceResult<string?>> CheckoutAsync(string buyerId, List<(Guid GameId, string RecipientId)> wanted, string successUrl, string cancelUrl)
        {
            var now = DateTime.UtcNow;
            var order = new Order { Id = Guid.NewGuid(), BuyerId = buyerId, CreatedAt = now, Status = OrderStatus.Pending };

            foreach (var (gameId, recipientId) in wanted.Distinct())
            {
                var game = _gameService.GetGameById(gameId);
                if (game == null) return ServiceResult<string?>.NotFound();
                if (game.IsDelisted) return ServiceResult<string?>.Invalid($"{game.Title} is no longer for sale.");

                if (_libraryRepository.Any(l => l.GamerId == recipientId && l.GameId == gameId))
                    return ServiceResult<string?>.Invalid(recipientId == buyerId
                        ? $"You already own {game.Title}."
                        : $"Your friend already owns {game.Title}.");

                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    GameId = game.Id,
                    GameTitle = game.Title ?? "Untitled",
                    Price = Pricing.CurrentPrice(game, now),
                    RecipientId = recipientId
                });
            }
            order.Total = order.Items.Sum(i => i.Price);

            if (order.Total > 0 && order.Total < StripeMinimumCharge)
                return ServiceResult<string?>.Invalid($"Card payments must be at least ${StripeMinimumCharge:0.00}. Add another game to your cart.");

            _orderRepository.Insert(order);

            if (order.Total <= 0)
            {
                Fulfil(order, paymentIntentId: null);
                return ServiceResult<string?>.Success(null, IsGift(order)
                    ? "Your gift has been sent!"
                    : order.Items.Count == 1 ? "Game added to your library." : "Games added to your library.");
            }

            var recipientNames = RecipientNames(order);
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ClientReferenceId = buyerId,
                // Set here on the server: the browser can't change what was bought or for whom
                Metadata = new Dictionary<string, string> { ["orderId"] = order.Id.ToString() },
                LineItems = order.Items.Where(i => i.Price > 0).Select(i => new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)Math.Round(i.Price * 100), // cents
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = i.RecipientId == buyerId ? i.GameTitle : $"{i.GameTitle} (gift for {recipientNames.GetValueOrDefault(i.RecipientId, "a friend")})"
                        }
                    }
                }).ToList()
            };

            try
            {
                var session = await Sessions().CreateAsync(options);
                order.StripeSessionId = session.Id;
                _orderRepository.Update(order);
                return ServiceResult<string?>.Success(session.Url);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Could not create a Stripe Checkout session for order {OrderId}", order.Id);
                order.Status = OrderStatus.Cancelled;
                _orderRepository.Update(order);
                return ServiceResult<string?>.Invalid("Payments are unavailable right now. Please try again later.");
            }
        }

        public async Task<ServiceResult> CompleteCheckoutAsync(string buyerId, string sessionId)
        {
            // Ask Stripe what actually happened instead of trusting the URL
            Session session;
            try
            {
                session = await Sessions().GetAsync(sessionId);
            }
            catch (StripeException)
            {
                return ServiceResult.NotFound();
            }

            var order = FindOrder(session);
            if (order == null) return ServiceResult.NotFound();
            if (order.BuyerId != buyerId) return ServiceResult.Forbidden();
            if (session.PaymentStatus != "paid") return ServiceResult.Invalid("Payment was not completed.");

            // Safe to repeat: the webhook may have got here first, or the page may be refreshed
            if (order.Status == OrderStatus.Pending) Fulfil(order, session.PaymentIntentId);

            return ServiceResult.Ok(IsGift(order)
                ? "Payment successful! Your gift has been sent."
                : "Payment successful! Your games are in your library.");
        }

        // ---------- Stripe webhook ----------

        public ServiceResult HandleStripeWebhook(string json, string? signature)
        {
            if (string.IsNullOrEmpty(_stripeSettings.WebhookSecret)) return ServiceResult.NotFound();
            if (string.IsNullOrEmpty(signature)) return ServiceResult.Invalid("Missing Stripe signature.");

            // The signature proves the request really comes from Stripe: anyone can POST to this URL
            Event stripeEvent;
            try
            {
                // Don't reject events whose API version differs from this Stripe.net build;
                // the dashboard's webhook version can be newer than the library
                stripeEvent = EventUtility.ConstructEvent(json, signature, _stripeSettings.WebhookSecret,
                    throwOnApiVersionMismatch: false);
            }
            catch (StripeException)
            {
                return ServiceResult.Invalid("Invalid Stripe signature.");
            }

            switch (stripeEvent.Type)
            {
                // Paid, even if the buyer closed the tab before coming back
                case "checkout.session.completed":
                case "checkout.session.async_payment_succeeded":
                    if (stripeEvent.Data.Object is Session paid && paid.PaymentStatus == "paid" &&
                        FindOrder(paid) is { Status: OrderStatus.Pending } toFulfil)
                        Fulfil(toFulfil, paid.PaymentIntentId);
                    break;

                // Abandoned checkout: Stripe expires it after 24 hours
                case "checkout.session.expired":
                    if (stripeEvent.Data.Object is Session expired && FindOrder(expired) is { Status: OrderStatus.Pending } toCancel)
                        SetStatus(toCancel, OrderStatus.Cancelled);
                    break;

                // Refunded from the Stripe dashboard (only full refunds take the games back)
                case "charge.refunded":
                    if (stripeEvent.Data.Object is Charge { Refunded: true } charge && charge.PaymentIntentId != null)
                    {
                        var order = _orderRepository.Get(o => o, o => o.PaymentIntentId == charge.PaymentIntentId,
                            include: q => q.Include(o => o.Items));
                        if (order is { Status: OrderStatus.Paid }) Refund(order);
                    }
                    break;
            }

            // Acknowledge everything, so Stripe doesn't keep retrying events we don't use
            return ServiceResult.Ok();
        }

        // ---------- History ----------

        public List<OrderDto> GetOrders(string buyerId)
        {
            var orders = _orderRepository.GetAll(
                selector: o => o,
                predicate: o => o.BuyerId == buyerId && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Refunded),
                orderBy: q => q.OrderByDescending(o => o.CreatedAt),
                include: q => q.Include(o => o.Items));

            var names = RecipientNames(orders.SelectMany(o => o.Items));
            return orders.Select(o => new OrderDto
            {
                Id = o.Id,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                Total = o.Total,
                Items = o.Items.Select(i => new OrderItemDto
                {
                    GameId = i.GameId,
                    GameTitle = i.GameTitle,
                    Price = i.Price,
                    GiftedTo = i.RecipientId == buyerId ? null : names.GetValueOrDefault(i.RecipientId, "Deleted user")
                }).ToList()
            }).ToList();
        }

        // ---------- Helpers ----------

        // Marks the order paid and hands out the games, all in one transaction. The order's Status is a
        // concurrency token: if the success page and the webhook race, the second save fails here and
        // nothing is granted twice.
        private void Fulfil(Order order, string? paymentIntentId)
        {
            // A recipient can delete their account between checkout and payment. Their items can't be
            // granted (and must not crash the webhook, or Stripe retries it forever): log them for a refund.
            var recipientIds = order.Items.Select(i => i.RecipientId).Distinct().ToList();
            var existing = _userManager.Users.Where(u => recipientIds.Contains(u.Id)).Select(u => u.Id).ToHashSet();
            var deliverable = order.Items.Where(i => existing.Contains(i.RecipientId)).ToList();
            if (deliverable.Count < order.Items.Count)
                _logger.LogWarning("Order {OrderId} was paid, but {Count} item(s) belong to deleted accounts and were not granted; refund them",
                    order.Id, order.Items.Count - deliverable.Count);

            try
            {
                _unitOfWork.InTransaction(() =>
                {
                    order.Status = OrderStatus.Paid;
                    order.PaymentIntentId = paymentIntentId;
                    _orderRepository.Update(order);

                    var now = DateTime.UtcNow;
                    foreach (var item in deliverable)
                    {
                        _libraryRepository.TryInsert(new Library
                        {
                            Id = Guid.NewGuid(),
                            GamerId = item.RecipientId,
                            GameId = item.GameId,
                            DateAdded = now
                        });

                        var wished = _wishlistRepository.GetAll(w => w, w => w.GamerId == item.RecipientId && w.GameId == item.GameId);
                        if (wished.Count > 0) _wishlistRepository.DeleteRange(wished);
                    }

                    var bought = order.Items.Where(i => i.RecipientId == order.BuyerId).Select(i => i.GameId).ToList();
                    var inCart = _cartRepository.GetAll(c => c, c => c.UserId == order.BuyerId && bought.Contains(c.GameId));
                    if (inCart.Count > 0) _cartRepository.DeleteRange(inCart);
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return; // the other request fulfilled it a moment ago
            }

            var buyerName = _userManager.Users.Where(u => u.Id == order.BuyerId).Select(u => u.UserName).FirstOrDefault() ?? "A friend";
            foreach (var gift in deliverable.Where(i => i.RecipientId != order.BuyerId))
                _notifications.Notify(gift.RecipientId, $"{buyerName} sent you {gift.GameTitle} as a gift!", $"/Libraries/Details/{gift.GameId}");
        }

        // Takes the games back from whoever got them and tells the buyer (and gift recipients)
        private void Refund(Order order)
        {
            try
            {
                _unitOfWork.InTransaction(() =>
                {
                    order.Status = OrderStatus.Refunded;
                    _orderRepository.Update(order);

                    foreach (var item in order.Items)
                    {
                        var owned = _libraryRepository.GetAll(l => l, l => l.GamerId == item.RecipientId && l.GameId == item.GameId);
                        if (owned.Count > 0) _libraryRepository.DeleteRange(owned);
                    }
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return;
            }

            if (order.BuyerId != null)
                _notifications.Notify(order.BuyerId, $"Your order of {string.Join(", ", order.Items.Select(i => i.GameTitle))} was refunded.", "/Orders");
            foreach (var gift in order.Items.Where(i => i.RecipientId != order.BuyerId))
                _notifications.Notify(gift.RecipientId, $"The gift {gift.GameTitle} was refunded by the sender and removed from your library.");
        }

        private void SetStatus(Order order, OrderStatus status)
        {
            order.Status = status;
            try { _orderRepository.Update(order); }
            catch (DbUpdateConcurrencyException) { /* already changed by another request */ }
        }

        // The order a Stripe session belongs to: the id in its metadata, and the session id must match
        private Order? FindOrder(Session session)
        {
            if (session.Metadata == null ||
                !session.Metadata.TryGetValue("orderId", out var orderIdText) ||
                !Guid.TryParse(orderIdText, out var orderId))
                return null;

            return _orderRepository.Get(o => o, o => o.Id == orderId && o.StripeSessionId == session.Id,
                include: q => q.Include(o => o.Items));
        }

        private static bool IsGift(Order order) => order.Items.Any(i => i.RecipientId != order.BuyerId);

        private Dictionary<string, string> RecipientNames(Order order) => RecipientNames(order.Items);

        private Dictionary<string, string> RecipientNames(IEnumerable<OrderItem> items)
        {
            var ids = items.Select(i => i.RecipientId).Distinct().ToList();
            return _userManager.Users.Where(u => ids.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.UserName ?? "Unknown");
        }

        private SessionService Sessions() => new SessionService(new StripeClient(_stripeSettings.SecretKey));
    }
}
