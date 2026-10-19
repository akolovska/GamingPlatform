using GamingPlatform.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace GamingPlatform.Web.Filters
{
    // Actions that buy, wishlist, befriend or publish need a confirmed email.
    // Unverified users are sent to their profile, where they can resend the confirmation email.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireVerifiedEmailAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var services = context.HttpContext.RequestServices;
            var userManager = services.GetRequiredService<UserManager<GamingPlatformUser>>();
            var user = await userManager.GetUserAsync(context.HttpContext.User);

            if (user != null && !user.EmailConfirmed)
            {
                var tempData = services.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(context.HttpContext);
                tempData["Warning"] = "Please verify your email to use this feature.";
                context.Result = new RedirectToActionResult("Index", "Account", null);
                return;
            }

            await next();
        }
    }
}
