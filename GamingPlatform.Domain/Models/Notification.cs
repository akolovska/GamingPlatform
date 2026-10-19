namespace GamingPlatform.Domain.Models
{
    public class Notification : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Url { get; set; }        // where clicking it goes
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
