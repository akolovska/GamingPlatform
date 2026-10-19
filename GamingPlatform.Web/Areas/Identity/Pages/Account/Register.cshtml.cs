#nullable disable

using System.ComponentModel.DataAnnotations;
using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Validation;
using Microsoft.AspNetCore.Authentication;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamingPlatform.Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        public const int MinimumAge = 18;

        private readonly SignInManager<GamingPlatformUser> _signInManager;
        private readonly UserManager<GamingPlatformUser> _userManager;
        private readonly IAccountService _accountService;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<GamingPlatformUser> userManager,
            SignInManager<GamingPlatformUser> signInManager,
            IAccountService accountService,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _accountService = accountService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        // "Continue with Google" (empty when Google isn't configured)
        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        // Limits for the date picker (yyyy-MM-dd, the format <input type="date"> uses)
        public string MinBirthDate => MinimumAgeAttribute.EarliestBirthDate.ToString("yyyy-MM-dd");
        public string MaxBirthDate => MinimumAgeAttribute.LatestBirthDate(MinimumAge).ToString("yyyy-MM-dd");

        public class InputModel
        {
            [Required, EmailAddress, StringLength(256)]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required, StringLength(50)]
            [Display(Name = "First name")]
            public string FirstName { get; set; }

            [Required, StringLength(50)]
            [Display(Name = "Last name")]
            public string LastName { get; set; }

            [Required, StringLength(30, MinimumLength = 3)]
            public string Username { get; set; }

            // Nullable so [Required] can tell "not entered" apart from a real date
            [Required(ErrorMessage = "Please enter your date of birth.")]
            [DataType(DataType.Date), MinimumAge(MinimumAge)]
            [Display(Name = "Date of birth")]
            public DateTime? DateOfBirth { get; set; }

            [StringLength(500)]
            public string Bio { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            var result = await _accountService.RegisterAsync(new RegisterDto
            {
                UserName = Input.Username,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                DateOfBirth = Input.DateOfBirth!.Value,
                Bio = Input.Bio,
                Password = Input.Password
            });
            if (!result.Succeeded)
            {
                foreach (var (_, message) in result.Errors)
                    ModelState.AddModelError(string.Empty, message);
                return Page();
            }

            var user = result.Value!;
            await _signInManager.SignInAsync(user, isPersistent: false);

            // A mail server problem doesn't stop the sign-up; the email can be resent from the profile
            bool sent = await _accountService.SendConfirmationEmailAsync(user.Id, (userId, code) =>
                Url.Page("/Account/ConfirmEmail", pageHandler: null,
                    values: new { area = "Identity", userId, code }, protocol: Request.Scheme));

            if (sent)
                TempData["Success"] = "Welcome! We sent a confirmation link to your email.";
            else
                TempData["Warning"] = "Welcome! We couldn't send the confirmation email. You can resend it from your profile.";

            return LocalRedirect(returnUrl);
        }
    }
}
