using System.Security.Claims;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Service;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    // Shared helpers for turning service results into responses
    public abstract class AppController : Controller
    {
        // The signed-in user's id (from the auth cookie, never from a form). Null when logged out.
        protected string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        protected bool IsAdmin => User.IsInRole(Roles.Admin);

        // NotFound -> 404, Forbidden -> 403. Invalid shouldn't get here (show it on the form instead).
        protected IActionResult Failure(ServiceResult result) => result.Status switch
        {
            ResultStatus.NotFound => NotFound(),
            ResultStatus.Forbidden => Forbid(),
            _ => BadRequest()
        };

        protected void AddErrors(ServiceResult result)
        {
            foreach (var (field, message) in result.Errors)
                ModelState.AddModelError(field, message);
        }

        // Shows the result's message as a toast on the next page
        protected void Flash(ServiceResult result)
        {
            if (string.IsNullOrEmpty(result.Message)) return;
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        }
    }
}
