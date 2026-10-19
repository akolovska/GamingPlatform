using System.Text;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace GamingPlatform.Service.Implementations
{
    public static class ImageFolders
    {
        public const string Covers = "covers";
        public const string Avatars = "avatars";
        public const string Screenshots = "screenshots";
    }

    public class ImageService : IImageService
    {
        private const long MaxBytes = 2 * 1024 * 1024; // 2 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly string _uploadsRoot;

        public ImageService(IWebHostEnvironment env)
        {
            _uploadsRoot = Path.Combine(env.WebRootPath, "uploads");
        }

        public string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0) return "Please choose an image.";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext) || file.Length > MaxBytes)
                return "Use a JPG, PNG or WebP image under 2 MB.";

            // The extension is the client's claim; the first bytes show what the file really is
            if (!LooksLikeImage(file))
                return "That file is not a valid JPG, PNG or WebP image.";

            return null;
        }

        public async Task<string> SaveAsync(IFormFile file, string folder)
        {
            var dir = Path.Combine(_uploadsRoot, folder);
            Directory.CreateDirectory(dir);

            // Never trust the client's filename; generate our own
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            await using (var stream = File.Create(Path.Combine(dir, fileName)))
            {
                await file.CopyToAsync(stream);
            }
            return $"/uploads/{folder}/{fileName}";
        }

        public void Delete(string? url, string folder)
        {
            var prefix = $"/uploads/{folder}/";
            if (string.IsNullOrEmpty(url) || !url.StartsWith(prefix, StringComparison.Ordinal)) return;

            // GetFullPath resolves "..", "./" and absolute paths, so after it we can check
            // that the file really sits directly inside our folder
            var dir = Path.GetFullPath(Path.Combine(_uploadsRoot, folder));
            var path = Path.GetFullPath(Path.Combine(dir, url[prefix.Length..]));
            if (!string.Equals(Path.GetDirectoryName(path), dir, StringComparison.OrdinalIgnoreCase)) return;

            if (File.Exists(path))
                File.Delete(path);
        }

        private static bool LooksLikeImage(IFormFile file)
        {
            var header = new byte[12];
            using var stream = file.OpenReadStream();
            int read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);

            bool jpeg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            bool png = read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            bool webp = read >= 12 && Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
            return jpeg || png || webp;
        }
    }
}
