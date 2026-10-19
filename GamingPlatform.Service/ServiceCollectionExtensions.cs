using GamingPlatform.Repository.Implementation;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Implementations;
using GamingPlatform.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Service
{
    public static class ServiceCollectionExtensions
    {
        // Repositories and business services. Used by Program.cs and by the test project,
        // so both build the app the same way.
        public static IServiceCollection AddGamingPlatformServices(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IAchievementService, AchievementService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IDevService, DevService>();
            services.AddScoped<IFriendService, FriendService>();
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IGenreService, GenreService>();
            services.AddScoped<ILibraryService, LibraryService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<ISaleAnnouncer, SaleAnnouncer>();
            services.AddScoped<IStudioService, StudioService>();
            services.AddScoped<IUserAdminService, UserAdminService>();
            services.AddScoped<IWishlistService, WishlistService>();

            services.AddHttpClient<IFreeToGameService, FreeToGameService>(client =>
            {
                client.BaseAddress = new Uri("https://www.freetogame.com/api/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            return services;
        }
    }
}
