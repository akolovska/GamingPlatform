using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace GamingPlatform.Web.Controllers
{
    [Authorize]
    public class AccountController : AppController
    {
        private const string AppName = "GamingPlatform"; // the name shown in authenticator apps

        private readonly IAccountService _accountService;
        private readonly UserManager<GamingPlatformUser> _userManager;
        private readonly SignInManager<GamingPlatformUser> _signInManager;

        public AccountController(
            IAccountService accountService,
            UserManager<GamingPlatformUser> userManager,
            SignInManager<GamingPlatformUser> signInManager)
        {
            _accountService = accountService;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ---------- Profiles ----------

        public IActionResult Index()
        {
            var profile = _accountService.GetMyProfile(UserId!);
            return profile == null ? NotFound() : View(profile);
        }

        // Another user's profile, by username
        public IActionResult Details(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return RedirectToAction(nameof(Index));

            var profile = _accountService.GetProfile(username, UserId!);
            if (profile == null)
            {
                TempData["Error"] = "There is no user with that username.";
                return RedirectToAction(nameof(Index));
            }
            if (profile.IsSelf) return RedirectToAction(nameof(Index));

            return View(profile);
        }

        // ---------- Edit profile ----------

        public async Task<IActionResult> Edit()
        {
            var dto = _accountService.GetProfileForEdit(UserId!);
            if (dto == null) return NotFound();

            await LoadSettingsAsync();
            return View(dto);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditDto dto)
        {
            if (ModelState.IsValid)
            {
                var result = await _accountService.UpdateProfileAsync(UserId!, dto);
                if (result.Succeeded)
                {
                    // Saving the email changes the security stamp; refresh the cookie so the user stays signed in
                    await RefreshSignInAsync();
                    if (result.Value) await SendConfirmationEmailAsync();
                    Flash(result);
                    return RedirectToAction(nameof(Index));
                }
                AddErrors(result);
            }

            await LoadSettingsAsync();
            return View(dto);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatarFile)
        {
            Flash(await _accountService.UpdateAvatarAsync(UserId!, avatarFile));
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAvatar()
        {
            await _accountService.RemoveAvatarAsync(UserId!);
            TempData["Success"] = "Profile picture removed.";
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmationEmail()
        {
            if (await SendConfirmationEmailAsync())
                TempData["Success"] = "Confirmation email sent. Check your inbox.";
            else
                TempData["Error"] = "We couldn't send the email. Your address may already be confirmed, or please try again later.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Password ----------

        public async Task<IActionResult> ChangePassword()
        {
            ViewBag.HasPassword = (await _accountService.GetSecurityStatusAsync(UserId!))?.HasPassword ?? true;
            return View(new ChangePasswordDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            bool hasPassword = (await _accountService.GetSecurityStatusAsync(UserId!))?.HasPassword ?? true;
            if (!hasPassword) ModelState.Remove(nameof(ChangePasswordDto.CurrentPassword)); // setting a first password

            if (ModelState.IsValid)
            {
                var result = await _accountService.ChangePasswordAsync(UserId!, dto);
                if (result.Succeeded)
                {
                    await RefreshSignInAsync();
                    Flash(result);
                    return RedirectToAction(nameof(Edit));
                }
                AddErrors(result);
            }
            ViewBag.HasPassword = hasPassword;
            return View(new ChangePasswordDto()); // never send passwords back to the browser
        }

        // ---------- Two-factor login ----------

        public async Task<IActionResult> EnableAuthenticator()
        {
            var setup = await _accountService.GetAuthenticatorSetupAsync(UserId!, AppName);
            if (setup == null) return NotFound();

            ViewBag.Setup = setup;
            ViewBag.QrCode = QrCodeDataUri(setup.AuthenticatorUri);
            return View(new TwoFactorCodeDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAuthenticator(TwoFactorCodeDto dto)
        {
            if (ModelState.IsValid)
            {
                var result = await _accountService.EnableTwoFactorAsync(UserId!, dto.Code);
                if (result.Succeeded)
                {
                    await RefreshSignInAsync();
                    Flash(result);
                    return ShowRecoveryCodes(result.Value!);
                }
                AddErrors(result);
            }

            var setup = await _accountService.GetAuthenticatorSetupAsync(UserId!, AppName);
            ViewBag.Setup = setup;
            ViewBag.QrCode = QrCodeDataUri(setup!.AuthenticatorUri);
            return View(new TwoFactorCodeDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor()
        {
            Flash(await _accountService.DisableTwoFactorAsync(UserId!));
            await RefreshSignInAsync();
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewRecoveryCodes()
        {
            var result = await _accountService.NewRecoveryCodesAsync(UserId!);
            Flash(result);
            return result.Succeeded ? ShowRecoveryCodes(result.Value!) : RedirectToAction(nameof(Edit));
        }

        // Shown once, right after they're made: they're stored only as hashes
        public IActionResult RecoveryCodes()
        {
            if (TempData["RecoveryCodes"] is not string codes) return RedirectToAction(nameof(Edit));
            return View(codes.Split(' '));
        }

        private IActionResult ShowRecoveryCodes(List<string> codes)
        {
            TempData["RecoveryCodes"] = string.Join(' ', codes);
            return RedirectToAction(nameof(RecoveryCodes));
        }

        // ---------- Connected accounts (Google) ----------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkLogin(string provider)
        {
            // Clear any half-finished external sign-in, then send the user to Google
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            var redirectUrl = Url.Action(nameof(LinkLoginCallback));
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, UserId);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> LinkLoginCallback()
        {
            // The userId check makes sure this Google login is linked to the account that asked for it
            var info = await _signInManager.GetExternalLoginInfoAsync(UserId);
            if (info == null)
            {
                TempData["Error"] = "Connecting failed. Please try again.";
                return RedirectToAction(nameof(Edit));
            }

            var result = await _accountService.LinkExternalLoginAsync(UserId!, info);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            await RefreshSignInAsync();
            Flash(result);
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(string provider)
        {
            Flash(await _accountService.RemoveExternalLoginAsync(UserId!, provider));
            await RefreshSignInAsync();
            return RedirectToAction(nameof(Edit));
        }

        // ---------- Delete account ----------

        public async Task<IActionResult> Delete()
        {
            ViewBag.HasPassword = (await _accountService.GetSecurityStatusAsync(UserId!))?.HasPassword ?? true;
            return View(new DeleteAccountDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(DeleteAccountDto dto)
        {
            if (ModelState.IsValid)
            {
                var result = await _accountService.DeleteAccountAsync(UserId!, dto.Password);
                if (result.Succeeded)
                {
                    await _signInManager.SignOutAsync();
                    TempData["Success"] = "Your account has been deleted.";
                    return RedirectToAction("Index", "Games");
                }
                AddErrors(result);
            }
            ViewBag.HasPassword = (await _accountService.GetSecurityStatusAsync(UserId!))?.HasPassword ?? true;
            return View(new DeleteAccountDto());
        }

        // ---------- Helpers ----------

        private async Task LoadSettingsAsync()
        {
            ViewBag.ProfilePictureUrl = _userManager.Users
                .Where(u => u.Id == UserId)
                .Select(u => u.ProfilePictureUrl)
                .FirstOrDefault();
            ViewBag.Security = await _accountService.GetSecurityStatusAsync(UserId!);
            ViewBag.ExternalProviders = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        private async Task RefreshSignInAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _signInManager.RefreshSignInAsync(user);
        }

        private Task<bool> SendConfirmationEmailAsync() =>
            _accountService.SendConfirmationEmailAsync(UserId!, (userId, code) =>
                Url.Page("/Account/ConfirmEmail", pageHandler: null,
                    values: new { area = "Identity", userId, code }, protocol: Request.Scheme)!);

        // The QR code is drawn here on the server, so the secret never goes to a third-party QR service
        private static string QrCodeDataUri(string text)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data).GetGraphic(6);
            return "data:image/png;base64," + Convert.ToBase64String(png);
        }
    }
}
