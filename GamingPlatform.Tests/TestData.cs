using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    // Builds the rows a test needs. Every name is unique, so tests never see each other's data.
    public static class TestData
    {
        public const string Password = "Test123!x";

        public static async Task<GamingPlatformUser> UserAsync(IServiceProvider services, string? role = null)
        {
            var name = "u" + Guid.NewGuid().ToString("N")[..12];
            var user = new GamingPlatformUser
            {
                UserName = name,
                Email = $"{name}@example.com",
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "User",
                DateOfBirth = new DateTime(1995, 1, 1)
            };

            var users = services.GetRequiredService<UserManager<GamingPlatformUser>>();
            var result = await users.CreateAsync(user, Password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            if (role != null) await users.AddToRoleAsync(user, role);
            return user;
        }

        public static Genre Genre(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var genre = new Genre { Id = Guid.NewGuid(), Name = "Genre " + Guid.NewGuid().ToString("N")[..8] };
            db.Genres.Add(genre);
            db.SaveChanges();
            return genre;
        }

        // A game, optionally published by a studio that `devUserId` manages
        public static Game Game(IServiceProvider services, decimal? price = 9.99m, bool delisted = false, string? devUserId = null,
            DateTime? releaseDate = null, int discountPercent = 0, Guid? genreId = null, string? title = null)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var game = new Game
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Game " + Guid.NewGuid().ToString("N")[..8],
                Price = price,
                IsDelisted = delisted,
                ReleaseDate = releaseDate,
                DiscountPercent = discountPercent
            };
            if (genreId != null)
                game.GameGenres.Add(new GameGenre { GenreId = genreId.Value });

            if (devUserId != null)
            {
                var studio = new Dev { Id = Guid.NewGuid(), StudioName = "Studio " + Guid.NewGuid().ToString("N")[..8], UserId = devUserId };
                db.Devs.Add(studio);
                game.GameDevelopers = new List<GameDev> { new GameDev { DevId = studio.Id } };
            }

            db.Games.Add(game);
            db.SaveChanges();
            return game;
        }

        public static void MakeFriends(IServiceProvider services, GamingPlatformUser a, GamingPlatformUser b)
        {
            var friends = services.GetRequiredService<GamingPlatform.Service.Interfaces.IFriendService>();
            friends.SendFriendRequest(a.Id, b.UserName!);
            friends.SendFriendRequest(b.Id, a.UserName!); // b asking back accepts it
        }

        // A paid checkout waiting for Stripe, as OrderService leaves it after creating the session
        public static Order PendingOrder(IServiceProvider services, string buyerId, string stripeSessionId, params (Game Game, string RecipientId)[] items)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var order = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                CreatedAt = DateTime.UtcNow,
                Status = Domain.Enum.OrderStatus.Pending,
                StripeSessionId = stripeSessionId,
                Items = items.Select(i => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    GameId = i.Game.Id,
                    GameTitle = i.Game.Title!,
                    Price = i.Game.Price ?? 0,
                    RecipientId = i.RecipientId
                }).ToList()
            };
            order.Total = order.Items.Sum(i => i.Price);
            db.Orders.Add(order);
            db.SaveChanges();
            return order;
        }

        public static void Own(IServiceProvider services, string userId, Guid gameId)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            db.Libraries.Add(new Library { Id = Guid.NewGuid(), GamerId = userId, GameId = gameId, DateAdded = DateTime.UtcNow });
            db.SaveChanges();
        }
    }
}
