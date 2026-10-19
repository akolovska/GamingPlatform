using Microsoft.AspNetCore.Http;

namespace GamingPlatform.Service.Interfaces
{
    // Stores uploaded images in wwwroot/uploads/{folder}
    public interface IImageService
    {
        // An error message for an unusable file, or null when it's fine
        string? Validate(IFormFile? file);

        // Returns the public URL of the saved file
        Task<string> SaveAsync(IFormFile file, string folder);

        // Deletes a file saved by SaveAsync. Anything else (external URLs, paths that
        // leave the folder) is ignored.
        void Delete(string? url, string folder);
    }
}
