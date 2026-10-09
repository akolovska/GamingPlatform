#nullable disable
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using GamingPlatform.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamingPlatform.Web.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<GamingPlatformUser> _userManager;
        private readonly SignInManager<GamingPlatformUser> _signInManager;

        public IndexModel(UserManager<GamingPlatformUser> userManager, SignInManager<GamingPlatformUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string Username { get; set; }

        public class InputModel
        {

            [Display(Name = "First name")]
            public string FirstName { get; set; }

            [Display(Name = "Last name")]
            public string LastName { get; set; }

            [Display(Name = "Date of birth")]
            [DataType(DataType.Date)]
            public DateTime? DateOfBirth { get; set; }

            [Display(Name = "Profile picture URL")]
            public string ProfilePictureUrl { get; set; }

            [Display(Name = "Bio")]
            public string Bio { get; set; }

            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(GamingPlatformUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            var phone = await _userManager.GetPhoneNumberAsync(user);

            Username = user.UserName;

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Bio = user.Bio,
                Email = email,
                PhoneNumber = phone
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            if (Input.Email != await _userManager.GetEmailAsync(user))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
                if (!setEmailResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Unexpected error when trying to set email.");
                    return Page();
                }
            }

            if (Input.PhoneNumber != await _userManager.GetPhoneNumberAsync(user))
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Unexpected error when trying to set phone number.");
                    return Page();
                }
            }

            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.DateOfBirth = Input.DateOfBirth;
            user.ProfilePictureUrl = Input.ProfilePictureUrl;
            user.Bio = Input.Bio;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Unexpected error when updating profile.");
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToPage();
        }
    }
}
