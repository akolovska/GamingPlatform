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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Friend>()
        .HasKey(f => f.Id); // assuming BaseEntity.Id exists

            // Relationship: Friend.User (requester) → GamingPlatformUser
            modelBuilder.Entity<Friend>()
                .HasOne(f => f.User)
                .WithMany(u => u.FriendshipsSent)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.ClientCascade); // EF deletes automatically in code

            // Relationship: Friend.FriendUser (receiver) → GamingPlatformUser
            modelBuilder.Entity<Friend>()
                .HasOne(f => f.FriendUser)
                .WithMany(u => u.FriendshipsReceived)
                .HasForeignKey(f => f.FriendId)
                .OnDelete(DeleteBehavior.ClientCascade); // EF deletes automatically in code

            // Optional: ensure CreatedAt is required
            modelBuilder.Entity<Friend>()
                .Property(f => f.CreatedAt)
                .IsRequired();
            modelBuilder.Entity<Game>()
                .Property(g => g.Price)
                .HasPrecision(18, 2);

        }
    }
}
