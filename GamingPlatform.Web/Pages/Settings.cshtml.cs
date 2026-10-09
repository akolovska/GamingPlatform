using GamingPlatform.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamingPlatform.Web.Pages
{
    [Authorize]
    public class SettingsModel : PageModel
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxBytes = 2 * 1024 * 1024; // 2 MB

        private readonly UserManager<GamingPlatformUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public SettingsModel(UserManager<GamingPlatformUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        [BindProperty]
        public IFormFile? AvatarFile { get; set; }

        public string? CurrentUrl { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            CurrentUrl = user.ProfilePictureUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostUploadAvatarAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            CurrentUrl = user.ProfilePictureUrl; // so the page shows the right image on errors

            if (AvatarFile == null || AvatarFile.Length == 0)
            {
                ModelState.AddModelError(nameof(AvatarFile), "Please choose a file.");
                return Page();
            }

            var ext = Path.GetExtension(AvatarFile.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext) || AvatarFile.Length > MaxBytes)
            {
                ModelState.AddModelError(nameof(AvatarFile), "Use a JPG, PNG or WebP under 2 MB.");
                return Page();
            }

            var folder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(folder);

            var fileName = $"{user.Id}_{Guid.NewGuid():N}{ext}";
            await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
            {
                await AvatarFile.CopyToAsync(stream);
            }

            DeleteAvatarFile(user.ProfilePictureUrl); // remove the old one

            user.ProfilePictureUrl = $"/uploads/avatars/{fileName}";
            await _userManager.UpdateAsync(user);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAvatarAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            DeleteAvatarFile(user.ProfilePictureUrl);

            user.ProfilePictureUrl = null; // falls back to the default avatar
            await _userManager.UpdateAsync(user);

            return RedirectToPage();
        }

        private void DeleteAvatarFile(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;

            var path = Path.Combine(_env.WebRootPath, url.TrimStart('/'));
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }
}