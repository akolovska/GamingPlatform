using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GamingPlatform.Repository.Data
{
    public class ApplicationDbContext : IdentityDbContext<GamingPlatformUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Dev> Devs { get; set; }
        public virtual DbSet<Game> Games { get; set; }
        public virtual DbSet<GameDev> GameDevs { get; set; }
        public virtual DbSet<Library> Libraries { get; set; }
        public virtual DbSet<Wishlist> Wishlists { get; set; }
        public virtual DbSet<Achievement> Achievements { get; set; }
        public virtual DbSet<Friend> Friends { get; set; }
        public virtual DbSet<Review> Reviews { get; set; }
        public virtual DbSet<Genre> Genres { get; set; }
        public virtual DbSet<GameGenre> GameGenres { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }
        public virtual DbSet<CartItem> CartItems { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<ReviewVote> ReviewVotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Orders are kept forever, so BuyerId/RecipientId have no foreign key to AspNetUsers:
            // deleting an account must not delete (or be blocked by) its purchase records.
            modelBuilder.Entity<Order>(e =>
            {
                e.Property(o => o.Total).HasPrecision(18, 2);
                // Fulfilling an order checks the status it was read with, so the success page and the
                // webhook can't both fulfil the same order (the second save fails and is ignored)
                e.Property(o => o.Status).IsConcurrencyToken();
                e.HasIndex(o => o.BuyerId);
                e.HasIndex(o => o.StripeSessionId).IsUnique().HasFilter("[StripeSessionId] IS NOT NULL");
                e.HasIndex(o => o.PaymentIntentId);
            });

            modelBuilder.Entity<OrderItem>(e =>
            {
                e.Property(i => i.Price).HasPrecision(18, 2);
                e.HasOne(i => i.Order).WithMany(o => o.Items)
                 .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(i => i.Game).WithMany()
                 .HasForeignKey(i => i.GameId).OnDelete(DeleteBehavior.Restrict); // a sold game's records stay
                e.HasIndex(i => i.RecipientId);
            });

            modelBuilder.Entity<CartItem>(e =>
            {
                e.HasOne<GamingPlatformUser>().WithMany()
                 .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(c => c.Game).WithMany()
                 .HasForeignKey(c => c.GameId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(c => new { c.UserId, c.GameId }).IsUnique();
            });

            modelBuilder.Entity<Notification>(e =>
            {
                e.HasOne<GamingPlatformUser>().WithMany()
                 .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(n => new { n.UserId, n.IsRead });
            });

            // No foreign key on UserId: users -> reviews (SET NULL) -> votes would be a second
            // cascade path, which SQL Server refuses. AccountService deletes a user's votes instead.
            modelBuilder.Entity<ReviewVote>(e =>
            {
                e.HasOne<Review>().WithMany()
                 .HasForeignKey(v => v.ReviewId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(v => new { v.ReviewId, v.UserId }).IsUnique();
                e.HasIndex(v => v.UserId);
            });

            modelBuilder.Entity<Dev>(e =>
            {
                e.HasOne(d => d.User)
                 .WithMany()                        // no collection needed on the user
                 .HasForeignKey(d => d.UserId)
                 .OnDelete(DeleteBehavior.SetNull); // deleting the account keeps the studio and its games

                // Optional: one studio per user (nulls are allowed multiple times)
                e.HasIndex(d => d.UserId)
                 .IsUnique()
                 .HasFilter("[UserId] IS NOT NULL");
            });

            modelBuilder.Entity<Friend>(e =>
            {
                e.HasOne(f => f.User)
                 .WithMany(u => u.FriendshipsSent)
                 .HasForeignKey(f => f.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(f => f.FriendUser)
                 .WithMany(u => u.FriendshipsReceived)
                 .HasForeignKey(f => f.FriendId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(f => new { f.UserId, f.FriendId }).IsUnique();
            });

            // One row per player per game. Deleting an account removes that player's
            // library and wishlist, but keeps their reviews with no author.
            modelBuilder.Entity<Library>(e =>
            {
                e.HasOne<GamingPlatformUser>().WithMany()
                 .HasForeignKey(l => l.GamerId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(l => new { l.GamerId, l.GameId }).IsUnique();
            });

            modelBuilder.Entity<Wishlist>(e =>
            {
                e.HasOne<GamingPlatformUser>().WithMany()
                 .HasForeignKey(w => w.GamerId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(w => new { w.GamerId, w.GameId }).IsUnique();
            });

            modelBuilder.Entity<Review>(e =>
            {
                e.HasOne<GamingPlatformUser>().WithMany()
                 .HasForeignKey(r => r.GamerId).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(r => new { r.GamerId, r.GameId }).IsUnique();
            });

            modelBuilder.Entity<Game>()
                .Property(g => g.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Genre>().HasIndex(g => g.Name).IsUnique();

            modelBuilder.Entity<GameGenre>(e =>
            {
                e.HasOne(x => x.Game).WithMany(g => g.GameGenres)
                 .HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Genre).WithMany(g => g.GameGenres)
                 .HasForeignKey(x => x.GenreId).OnDelete(DeleteBehavior.Restrict); // can't delete a genre in use
                e.HasIndex(x => new { x.GameId, x.GenreId }).IsUnique();
            });
        }   

    }
}
