using GamingPlatform.Domain.Enum;

namespace GamingPlatform.Domain.Models
{
    // A purchase: one or more games, each for the buyer or as a gift.
    // Orders are records of what happened, so they are never edited except for their status,
    // and they stay when an account is deleted (BuyerId then points at no one).
    public class Order : BaseEntity
    {
        public string? BuyerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Total { get; set; }

        public string? StripeSessionId { get; set; }   // null for free orders
        public string? PaymentIntentId { get; set; }   // set when paid; refunds are matched by it

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public class OrderItem : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public Guid GameId { get; set; }
        public Game Game { get; set; } = null!;

        // Copied at checkout: the receipt must show what was paid, even if the game changes later
        public string GameTitle { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public string RecipientId { get; set; } = string.Empty; // the buyer, or the friend it was gifted to
    }
}
