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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GamerFriend>()
                .HasKey(f => new { f.GamerId, f.FriendId });

            modelBuilder.Entity<GamerFriend>()
                .HasOne(f => f.Gamer)
                .WithMany(g => g.Friends)
                .HasForeignKey(f => f.GamerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<GamerFriend>()
                .HasOne(f => f.Friend)
                .WithMany(g => g.FriendOf)
                .HasForeignKey(f => f.FriendId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Game>()
                .Property(g => g.Price)
                .HasPrecision(10, 2); // 10 total digits, 2 after decimal

            // other model configurations...
        }

        public DbSet<Dev> Devs { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<GameDev> GameDevs { get; set; }
        public DbSet<HighScore> HighScores { get; set; }
        public DbSet<Gamer> Gamers { get; set; }
        public DbSet<GamerGame> GamerGames { get; set; }
        public DbSet<GamerFriend> GamerFriends { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }


    }
}
