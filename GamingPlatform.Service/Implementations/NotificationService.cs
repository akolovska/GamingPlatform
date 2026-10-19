using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using Microsoft.AspNetCore.Identity;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Service.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IRepository<Notification> _notificationRepository;
        private readonly UserManager<GamingPlatformUser> _userManager;

        public NotificationService(IRepository<Notification> notificationRepository, UserManager<GamingPlatformUser> userManager)
        {
            _notificationRepository = notificationRepository;
            _userManager = userManager;
        }

        public void Notify(string userId, string message, string? url = null) =>
            NotifyMany(new[] { userId }, message, url);

        public void NotifyMany(IEnumerable<string> userIds, string message, string? url = null)
        {
            // Skip accounts that no longer exist (e.g. a refund for someone who deleted their account):
            // their notification would only break the foreign key and fail the whole request
            var wanted = userIds.Distinct().ToList();
            var existing = _userManager.Users.Where(u => wanted.Contains(u.Id)).Select(u => u.Id).ToList();

            var now = DateTime.UtcNow;
            var rows = existing
                .Select(id => new Notification { Id = Guid.NewGuid(), UserId = id, Message = message, Url = url, CreatedAt = now })
                .ToList();
            if (rows.Count > 0) _notificationRepository.InsertRange(rows);
        }

        public int UnreadCount(string userId) =>
            _notificationRepository.Count(n => n.UserId == userId && !n.IsRead);

        public List<NotificationDto> GetRecent(string userId, int take = 50) =>
            _notificationRepository.GetAll(
                selector: n => new NotificationDto { Id = n.Id, Message = n.Message, Url = n.Url, CreatedAt = n.CreatedAt, IsRead = n.IsRead },
                predicate: n => n.UserId == userId,
                orderBy: q => q.OrderByDescending(n => n.CreatedAt),
                take: take);

        public void MarkAllRead(string userId)
        {
            var unread = _notificationRepository.GetAll(n => n, n => n.UserId == userId && !n.IsRead);
            if (unread.Count == 0) return;
            foreach (var n in unread) n.IsRead = true;
            _notificationRepository.UpdateRange(unread);
        }
    }
}
