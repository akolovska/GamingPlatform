using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Web.Jobs
{
    // Runs in the background while the app is up. Sales start at midnight (UTC) on their first day,
    // so once an hour (the default; Jobs:SaleAnnouncementMinutes changes it) announces them on the day they begin.
    public class SaleAnnouncementJob : BackgroundService
    {
        private readonly TimeSpan _interval;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<SaleAnnouncementJob> _logger;

        public SaleAnnouncementJob(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<SaleAnnouncementJob> logger)
        {
            _scopes = scopes;
            _logger = logger;
            _interval = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Jobs:SaleAnnouncementMinutes", 60)));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // A short wait first, so the app finishes starting before the first run
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { return; }

            using var timer = new PeriodicTimer(_interval);
            do
            {
                try
                {
                    // Services and the DbContext are per request; a job makes its own scope for each run
                    using var scope = _scopes.CreateScope();
                    int count = scope.ServiceProvider.GetRequiredService<ISaleAnnouncer>().AnnounceStartedSales();
                    if (count > 0) _logger.LogInformation("Announced {Count} sale(s)", count);
                }
                catch (Exception ex)
                {
                    // One failed run (e.g. the database was briefly down) mustn't stop the job for good
                    _logger.LogError(ex, "The sale announcement job failed; it will try again at its next run");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
