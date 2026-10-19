using GamingPlatform.Service.Implementations;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    [Collection("Database")]
    public class ImageServiceTests
    {
        // The smallest valid PNG: 1x1 pixel
        private static readonly byte[] Png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        private readonly TestDatabase _db;
        private readonly IImageService _images;

        public ImageServiceTests(TestDatabase db)
        {
            _db = db;
            _images = db.Services.CreateScope().ServiceProvider.GetRequiredService<IImageService>();
        }

        private static IFormFile File(byte[] bytes, string name) =>
            new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name);

        [Fact]
        public void A_real_png_is_accepted() =>
            Assert.Null(_images.Validate(File(Png, "avatar.png")));

        [Fact]
        public void A_text_file_renamed_to_png_is_rejected() =>
            Assert.Contains("not a valid", _images.Validate(File("<script>alert(1)</script>"u8.ToArray(), "evil.png")));

        [Fact]
        public void Other_extensions_are_rejected() =>
            Assert.NotNull(_images.Validate(File(Png, "avatar.svg")));

        [Fact]
        public void Files_over_2MB_are_rejected()
        {
            var big = new byte[2 * 1024 * 1024 + 1];
            Png.CopyTo(big, 0);
            Assert.NotNull(_images.Validate(File(big, "big.png")));
        }

        [Fact]
        public async Task Saved_files_can_be_deleted_again()
        {
            var url = await _images.SaveAsync(File(Png, "a.png"), ImageFolders.Avatars);
            var path = Path.Combine(_db.WebRoot, url.TrimStart('/'));
            Assert.True(System.IO.File.Exists(path));

            _images.Delete(url, ImageFolders.Avatars);
            Assert.False(System.IO.File.Exists(path));
        }

        // The bug that let any user delete any file: these URLs must never delete the canary
        [Theory]
        [InlineData("/uploads/avatars/../../canary.txt")]
        [InlineData("/uploads/avatars/..\\..\\canary.txt")]
        [InlineData("/uploads/avatars/../covers/../../canary.txt")]
        [InlineData("../canary.txt")]
        [InlineData("canary.txt")]
        [InlineData("/uploads/covers/../../canary.txt")]
        public void Paths_outside_the_folder_are_never_deleted(string url)
        {
            var canary = Path.Combine(_db.WebRoot, "canary.txt");
            System.IO.File.WriteAllText(canary, "keep me");

            _images.Delete(url, ImageFolders.Avatars);

            Assert.True(System.IO.File.Exists(canary));
        }

        [Fact]
        public void Absolute_paths_are_never_deleted()
        {
            var canary = Path.Combine(_db.WebRoot, "canary-abs.txt");
            System.IO.File.WriteAllText(canary, "keep me");

            _images.Delete(canary, ImageFolders.Avatars);                       // C:\...\canary-abs.txt
            _images.Delete("/uploads/avatars/" + canary, ImageFolders.Avatars);  // prefix + absolute path

            Assert.True(System.IO.File.Exists(canary));
        }
    }
}
