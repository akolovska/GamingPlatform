#nullable disable
using GamingPlatform.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamingPlatform.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<GamingPlatformUser> _signInManager;

        public LogoutModel(SignInManager<GamingPlatformUser> signInManager)
        {
            _signInManager = signInManager;
        }

        // Logging out only works by POST (the navbar button), so a link on another site can't log people out
        public IActionResult OnGet() => RedirectToAction("Index", "Games", new { area = "" });

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            return returnUrl != null
                ? LocalRedirect(returnUrl)
                : RedirectToAction("Index", "Games", new { area = "" });
        }
    }
}
