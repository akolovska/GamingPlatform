using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.FileProviders;

namespace GamingPlatform.Tests
{
    // Records emails instead of sending them
    public class FakeEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = new();

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            lock (Sent) Sent.Add((email, subject, htmlMessage));
            return Task.CompletedTask;
        }
    }

    // Points wwwroot at a temporary folder so uploads in tests never touch the real site
    public class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string webRoot)
        {
            WebRootPath = webRoot;
            ContentRootPath = webRoot;
        }

        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "GamingPlatform.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Testing";
    }
}
