namespace GamingPlatform.Service.Interfaces
{
    // Tells wishlisters when a game goes on sale: a notification for everyone, and an email for
    // those with a confirmed address who haven't turned sale emails off. Each sale is announced once.
    public interface ISaleAnnouncer
    {
        // Announces this game's sale if it has started and hasn't been announced yet
        void Announce(Guid gameId);

        // Announces every sale that has started since the last run (scheduled sales). Returns how many.
        int AnnounceStartedSales();
    }
}
