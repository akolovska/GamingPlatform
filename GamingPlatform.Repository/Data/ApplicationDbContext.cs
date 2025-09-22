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
        public DbSet<HighScore> HighScores { get; set; }
        public DbSet<Gamer> Gamers { get; set; }
        public DbSet<Library> GamerGames { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }


    }
}
