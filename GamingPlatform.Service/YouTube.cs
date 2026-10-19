using System.Text.RegularExpressions;

namespace GamingPlatform.Service
{
    // Trailers are YouTube links. Only the 11-character video id is kept, so whatever a dev types,
    // the page only ever embeds https://www.youtube-nocookie.com/embed/{id}.
    public static partial class YouTube
    {
        [GeneratedRegex(@"^(?:https?://)?(?:www\.|m\.)?(?:youtube\.com/(?:watch\?(?:.*&)?v=|embed/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})(?:[?&#].*)?$")]
        private static partial Regex VideoLink();

        public static string? VideoId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var match = VideoLink().Match(url.Trim());
            return match.Success ? match.Groups[1].Value : null;
        }

        public static string WatchUrl(string videoId) => $"https://www.youtube.com/watch?v={videoId}";

        // youtube-nocookie.com doesn't set tracking cookies until the visitor presses play
        public static string? EmbedUrl(string? url) =>
            VideoId(url) is { } id ? $"https://www.youtube-nocookie.com/embed/{id}" : null;
    }
}
