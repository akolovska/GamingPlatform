using GamingPlatform.Domain.DTO;

namespace GamingPlatform.Service.Interfaces
{
    // The bell in the navbar
    public interface INotificationService
    {
        void Notify(string userId, string message, string? url = null);
        void NotifyMany(IEnumerable<string> userIds, string message, string? url = null);

        int UnreadCount(string userId);
        List<NotificationDto> GetRecent(string userId, int take = 50);
        void MarkAllRead(string userId);
    }
}
