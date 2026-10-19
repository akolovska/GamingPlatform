using GamingPlatform.Domain.Enum;

namespace GamingPlatform.Domain.DTO
{
    public class CartDto
    {
        public List<CartItemDto> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Price);
    }

    public class CartItemDto
    {
        public Guid GameId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public decimal Price { get; set; }          // what it costs today
        public decimal? FullPrice { get; set; }     // shown struck through when on sale
        public int DiscountPercent { get; set; }
        public bool IsComingSoon { get; set; }
    }

    public class OrderDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Total { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public Guid GameId { get; set; }
        public string GameTitle { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? GiftedTo { get; set; }       // null when bought for yourself
    }

    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
