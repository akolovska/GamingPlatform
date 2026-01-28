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

        public DbSet<Dev> Devs { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<GameDev> GameDevs { get; set; }
        public DbSet<Library> Libraries { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<Friend> Friends { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<GamerAchievement> GamerAchievements { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Friend>()
                .HasOne<GamingPlatformUser>()
                .WithMany(u => u.FriendshipsSent)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friend>()
                .HasOne<GamingPlatformUser>()
                .WithMany(u => u.FriendshipsReceived)
                .HasForeignKey(f => f.FriendId)
                .OnDelete(DeleteBehavior.Restrict);


            // Configure decimal precision for Price
            modelBuilder.Entity<Game>()
                .Property(g => g.Price)
                .HasPrecision(18, 2); // 18 total digits, 2 after decimal point

            modelBuilder.Entity<Friend>()
                .HasOne<GamingPlatformUser>() // the requester
                .WithMany(u => u.FriendshipsSent)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friend>()
                .HasOne<GamingPlatformUser>() // the receiver
                .WithMany(u => u.FriendshipsReceived)
                .HasForeignKey(f => f.FriendId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
