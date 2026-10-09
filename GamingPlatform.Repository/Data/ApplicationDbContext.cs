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
        public virtual DbSet<GamerAchievement> GamerAchievements { get; set; }
        public virtual DbSet<Genre> Genres { get; set; }
        public virtual DbSet<GameGenre> GameGenres { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
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
