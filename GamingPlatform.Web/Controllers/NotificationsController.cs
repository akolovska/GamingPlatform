using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    [Authorize]
    public class NotificationsController : AppController
    {
        private readonly INotificationService _notifications;

        public NotificationsController(INotificationService notifications)
        {
            _notifications = notifications;
        }

        // Opening the list counts as reading them; the page still highlights the ones that were new
        public IActionResult Index()
        {
            var recent = _notifications.GetRecent(UserId!);
            _notifications.MarkAllRead(UserId!);
            return View(recent);
        }
    }
}
