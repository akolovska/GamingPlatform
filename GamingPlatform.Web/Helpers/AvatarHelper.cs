namespace GamingPlatform.Web.Helpers
{
    public static class AvatarHelper
    {
        public const string Default = "/images/default-avatar.png";
        public static string Resolve(string? url) => string.IsNullOrEmpty(url) ? Default : url;
    }
}
