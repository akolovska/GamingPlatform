namespace GamingPlatform.Domain.Enum
{
    public enum OrderStatus
    {
        Pending,    // waiting for Stripe
        Paid,       // games granted
        Refunded,   // money returned, games taken back
        Cancelled   // the Stripe checkout expired unpaid
    }
}
