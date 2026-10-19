namespace GamingPlatform.Domain.Models
{
    public class StripeSettings
    {
        public string? SecretKey { get; set; }

        // Signing secret of the webhook endpoint (whsec_...). Without it the webhook is switched off.
        public string? WebhookSecret { get; set; }
    }
}
