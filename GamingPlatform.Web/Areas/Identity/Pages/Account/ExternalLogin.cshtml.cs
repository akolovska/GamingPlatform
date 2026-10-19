#nullable disable
using System.Security.Claims;
using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamingPlatform.Web.Areas.Identity.Pages.Account
{
    // "Continue with Google". Replaces Identity's built-in page, which would create an account from
    // just an email: here a first-time user also picks a username and gives a date of birth (18+).
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<GamingPlatformUser> _signInManager;
        private readonly IAccountService _accountService;

        public ExternalLoginModel(SignInManager<GamingPlatformUser> signInManager, IAccountService accountService)
        {
            _signInManager = signInManager;
            _accountService = accountService;
        }

        [BindProperty]
        public ExternalRegisterDto Input { get; set; }

        public string ProviderDisplayName { get; set; }
        public string ReturnUrl { get; set; }

        public IActionResult OnGet() => RedirectToPage("./Login");

        // Step 1: send the user to Google
        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        // Step 2: Google sends them back
        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            if (remoteError != null)
                return BackToLogin($"Google sign-in failed: {remoteError}", returnUrl);

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return BackToLogin("Google sign-in failed. Please try again.", returnUrl);

            // Already connected to an account: sign in (two-factor login still applies)
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);
            if (result.Succeeded) return LocalRedirect(returnUrl);
            if (result.RequiresTwoFactor) return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl });
            if (result.IsLockedOut) return RedirectToPage("./Lockout");

            // First time: ask for the details Google doesn't give us, starting from what it does
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;
            Input = new ExternalRegisterDto
            {
                Email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
                LastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty
            };
            return Page();
        }

        // Step 3: create the account
        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;

            // The Google login is kept in a temporary cookie between the steps
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return BackToLogin("Your Google sign-in expired. Please try again.", returnUrl);
            ProviderDisplayName = info.ProviderDisplayName;

            if (!ModelState.IsValid) return Page();

            var result = await _accountService.RegisterExternalAsync(Input, info);
            if (!result.Succeeded)
            {
                foreach (var (field, message) in result.Errors)
                    ModelState.AddModelError(string.IsNullOrEmpty(field) ? string.Empty : $"{nameof(Input)}.{field}", message);
                return Page();
            }

            await _signInManager.SignInAsync(result.Value!, isPersistent: false, info.LoginProvider);
            TempData["Success"] = "Welcome to GamingPlatform!";
            return LocalRedirect(returnUrl);
        }

        private IActionResult BackToLogin(string error, string returnUrl)
        {
            TempData["Error"] = error;
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }
    }
}
