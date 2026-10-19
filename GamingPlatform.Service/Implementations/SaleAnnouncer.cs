using System.Text.Encodings.Web;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GamingPlatform.Service.Implementations
{
    public class SaleAnnouncer : ISaleAnnouncer
    {
        private readonly IRepository<Game> _gameRepository;
        private readonly IRepository<Wishlist> _wishlistRepository;
        private readonly INotificationService _notifications;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<GamingPlatformUser> _userManager;
        private readonly string _siteUrl;
        private readonly ILogger<SaleAnnouncer> _logger;

        public SaleAnnouncer(
            IRepository<Game> gameRepository,
            IRepository<Wishlist> wishlistRepository,
            INotificationService notifications,
            IEmailSender emailSender,
            UserManager<GamingPlatformUser> userManager,
            IConfiguration configuration,
            ILogger<SaleAnnouncer> logger)
        {
            _gameRepository = gameRepository;
            _wishlistRepository = wishlistRepository;
            _notifications = notifications;
            _emailSender = emailSender;
            _userManager = userManager;
            // Emails are sent outside any web request, so links need the site's address from config
            _siteUrl = (configuration["SiteUrl"] ?? "https://localhost:7002").TrimEnd('/');
            _logger = logger;
        }

        public void Announce(Guid gameId)
        {
            var now = DateTime.UtcNow;
            var game = _gameRepository.Get(g => g, g => g.Id == gameId);
            if (game != null && IsDue(game, now)) AnnounceGame(game, now);
        }

        public int AnnounceStartedSales()
        {
            var now = DateTime.UtcNow;
            var onSale = Pricing.OnSaleInSql(now);
            var due = _gameRepository.GetAll(g => g, onSale)
                .Where(g => IsDue(g, now))
                .ToList();

            foreach (var game in due) AnnounceGame(game, now);
            return due.Count;
        }

        private static bool IsDue(Game game, DateTime now) =>
            game.SaleAnnouncedAt == null && !game.IsDelisted && (game.Price ?? 0) > 0 && Pricing.IsOnSale(game, now);

        private void AnnounceGame(Game game, DateTime now)
        {
            // Marked first, so a crash halfway or two runs at once can't announce it twice
            game.SaleAnnouncedAt = now;
            _gameRepository.Update(game);

            var wisherIds = _wishlistRepository.GetAll(w => w.GamerId!, w => w.GameId == game.Id && w.GamerId != null);
            if (wisherIds.Count == 0) return;

            var until = game.DiscountEndsAt is { } end ? $" until {end.AddDays(-1):MMMM d}" : "";
            var gameUrl = $"/Games/Details/{game.Id}";
            _notifications.NotifyMany(wisherIds, $"{game.Title} from your wishlist is on sale: -{game.DiscountPercent}%{until}!", gameUrl);

            // Email only confirmed addresses, and only people who still want these emails
            var recipients = _userManager.Users
                .Where(u => wisherIds.Contains(u.Id) && u.EmailConfirmed && u.WishlistSaleEmails && u.Email != null)
                .Select(u => new { u.Email, u.UserName })
                .ToList();

            var title = HtmlEncoder.Default.Encode(game.Title ?? "A game");
            var price = Pricing.CurrentPrice(game, now);
            int sent = 0;
            foreach (var r in recipients)
            {
                var body =
                    $"Hello {HtmlEncoder.Default.Encode(r.UserName ?? "")},<br/><br/>" +
                    $"<strong>{title}</strong> from your wishlist is <strong>-{game.DiscountPercent}%</strong>{until}: now ${price:0.00}.<br/><br/>" +
                    $"<a href='{_siteUrl}{gameUrl}'>See it in the store</a><br/><br/>" +
                    $"<small>Don't want these emails? Turn them off in <a href='{_siteUrl}/Account/Edit'>Account settings</a>.</small>";
                try
                {
                    _emailSender.SendEmailAsync(r.Email!, $"{game.Title} is on sale: -{game.DiscountPercent}%", body)
                        .GetAwaiter().GetResult();
                    sent++;
                }
                catch (Exception ex)
                {
                    // One bad address or a mail server hiccup mustn't stop the other emails
                    _logger.LogWarning(ex, "Could not send the sale email for game {GameId}", game.Id);
                }
            }
            _logger.LogInformation("Announced the sale of {GameId} to {Count} wishlisters: {Sent} email(s) sent, {Failed} failed",
                game.Id, wisherIds.Count, sent, recipients.Count - sent);
        }
    }
}
