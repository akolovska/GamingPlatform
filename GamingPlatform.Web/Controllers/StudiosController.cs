using GamingPlatform.Domain.Identity;
using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    public class StudiosController : AppController
    {
        private readonly IStudioService _studioService;

        public StudiosController(IStudioService studioService)
        {
            _studioService = studioService;
        }

        // The public page of a studio and its games
        [AllowAnonymous]
        public IActionResult Details(Guid id)
        {
            var page = _studioService.GetPage(id, UserId, IsAdmin);
            return page == null ? NotFound() : View(page);
        }

        // How a studio's games are doing. Devs see their own studio; admins pick any (?id=...).
        [Authorize(Roles = Roles.DevOrAdmin)]
        public IActionResult Dashboard(Guid? id)
        {
            var result = _studioService.GetDashboard(id, UserId!, IsAdmin);
            if (result.Status == ResultStatus.Invalid)
            {
                Flash(result);
                return RedirectToAction("Index", "Games");
            }
            if (!result.Succeeded) return Failure(result);

            return View(result.Value);
        }
    }
}
